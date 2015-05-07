﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EditorExtensions
{
	public static class CompoundPartUtil
	{
		const string VectorFormat = "F3";

		static CompoundPartUtil()
		{
		}

		//get distance: Vector3.Distance(object1.transform.position, object2.transform.position);
		//Physics.Raycast(this.transform.position, this.transform.TransformDirection(dir), out this.\u0001\u0002, this.maxLength, EditorLogic.LayerMask)

		const string strutPartName = "strutConnector";
		const string fuelLinePartName = "fuelLine";
		const float CompoundPartMaxLength = 10f;

		/// <summary>
		/// Align compount part, leave in starting position but center and level to target
		/// </summary>
		/// <param name="part">Part</param>
		public static void AlignCompoundPart (CompoundPart part)
		{
			if (part.parent == null || part.target == null) {
				Log.Debug("Part is not fully connected");
				return;
			}

			Part startPart = part.parent;
			Part targetPart = part.target;

			Log.Debug("Getting parentLocalHeight");
			float parentLocalHeight = GetCompoundPartPositionHeight(part, part.parent);
			//float parentLocalHeight = SnapCompoundPartHeight(part, part.parent);
			Log.Debug("Getting targetLocalHeight");
			float targetLocalHeight = GetCompoundPartPositionHeight(part, part.target);
			//float targetLocalHeight = SnapCompoundPartHeight(part, part.target);

			Vector3 startPosition = startPart.transform.position;
			Vector3 destPosition = targetPart.transform.position;

			Log.Debug(string.Format("startPosition: {0} destPosition: {1}", startPosition.ToString(), destPosition.ToString()));
			//center to center
			//GetEndpoints(startPart, targetPart, midway, out startPosition, out destPosition);

			GetCollisionPointOnAxis(startPart, destPosition, parentLocalHeight, out startPosition);
			GetCollisionPointOnAxis(targetPart, startPosition, targetLocalHeight, out destPosition);
			Log.Debug(string.Format("new collider startPosition: {0} destPosition: {1}", startPosition.ToString(), destPosition.ToString()));
			
			part.transform.position = startPosition;
			part.transform.up = startPart.transform.up;
			part.transform.forward = startPart.transform.forward;
			part.transform.LookAt(targetPart.transform); //this rotates the strut base towards the target, need to keep it flush with the parent
			part.transform.Rotate(0, 90, 0);

			//Vector3 dirToTarget = part.transform.InverseTransformPoint(destPosition).normalized;
			//Vector3 dirToTarget = (destPosition - startPosition).normalized;
			Vector3 dirToTarget = part.transform.InverseTransformPoint(destPosition).normalized;
			Log.Debug("final direction: " + startPosition.ToString(VectorFormat));

			part.raycastTarget(dirToTarget);
		}

		static float GetCompoundPartPositionHeight (CompoundPart part, Part target)
		{
			Log.Debug(string.Format("Getting new position height for {0} on {1}", part.name, target.name));

			//lengthwise position on parent (extents, +/- from center)
			float localHeight = 0f;
			float parentHeight = 0f;
			if (part.parent == target) {
				localHeight = part.transform.localPosition.y;
				parentHeight = part.parent.GetPartRendererBound ().extents.y;
			} else if (part.target == target) {
				Vector3 targetPos = part.transform.TransformPoint (part.targetPosition);
				targetPos = target.transform.InverseTransformPoint (targetPos);
				localHeight = targetPos.y;
				parentHeight = target.GetPartRendererBound().extents.y;
			} else {
				Log.Warn ("Unable to identify part");
			}

			Log.Debug(string.Format("localHeight: {0} parentHeight: {1}", localHeight.ToString("F3"), parentHeight.ToString("F3")));

			//offset strut when attaching to top/bottom of parent
			const float strutOffset = 0.15f;
			//threshold for small parent parts
			const float parentSizeCutoff = 0.5f;

			float heightPct = localHeight / parentHeight;
			Log.Debug("Attachment height%: " + (heightPct * 100f).ToString("F0"));
			Log.Debug("Original localHeight: " + localHeight.ToString());

			//+1 up, -1 down
			float upOrDown = 1f;
			if (localHeight < 0f)
				upOrDown = -1f;

			if (parentHeight < parentSizeCutoff) {
				//for small parts, just center on them
				Log.Debug("Parent is small, defaulting to center");
				localHeight = 0f;
			} else if (Math.Abs(localHeight) < parentHeight * 0.5f) {
				//middle 50% of parent, snap to center
				Log.Debug("Centering on parent");
				localHeight = 0f;
			} else {
				//top/bottom edge
				Log.Debug("Aligning to edge of parent");
				localHeight = (parentHeight - strutOffset) * upOrDown;
			}
			Log.Debug("new localHeight: " + localHeight.ToString());
			return localHeight;
		}

		/// <summary>
		/// get coll point
		/// </summary>
		/// <returns><c>true</c>, if collision point on axis2 was gotten, <c>false</c> otherwise.</returns>
		/// <param name="part">Part to get surface attachment point</param>
		/// <param name="externalPoint">Other part for direction</param>
		/// <param name="partHeight">Height to attach on part</param>
		/// <param name="collisionPoint">Collision point on part</param>
		private static bool GetCollisionPointOnAxis (Part part, Vector3 externalPoint, float partHeight, out Vector3 collisionPoint)
		{
			//(to - from).normalized;
			Log.Debug ("partHeight " + partHeight.ToString ("F3"));
			Vector3 internalTarget = part.transform.TransformPoint (new Vector3 (0f, partHeight, 0f));
			Log.Debug ("internalTarget " + internalTarget.ToString (VectorFormat));
			//localize external point
			externalPoint = part.transform.InverseTransformPoint (externalPoint);
			Log.Debug ("local externalPoint " + externalPoint.ToString (VectorFormat));
			//level to same height
			externalPoint.y = 0f;
			Log.Debug ("levelled local externalPoint " + externalPoint.ToString (VectorFormat));
			//back to global
			externalPoint = part.transform.TransformPoint (externalPoint);
			Log.Debug ("global externalPoint " + externalPoint.ToString (VectorFormat));

			Vector3 direction = (internalTarget - externalPoint).normalized;
			Log.Debug ("direction " + direction.ToString (VectorFormat));

			return GetPartCollisionPointByDirection (part, externalPoint, direction, out collisionPoint);
		}

		/// <summary>
		/// get collision point starting from inside the part's collider
		/// </summary>
		private static bool GetPartCollisionPointByDirection (Part part, Vector3 origin, Vector3 targetDirection, out Vector3 collisionPoint)
		{
			Log.Debug (string.Format("GetPartCollisionPointByDirection: collider raycast from {0} in direction {1}", origin.ToString (VectorFormat), targetDirection.ToString (VectorFormat)));
			//using this method becuase Physics.RayCast ignores hits originating from inside the collider
			Ray r = new Ray (origin, targetDirection);

			//collider property is deprecated, removed in 5.0 - use GetComponent<Collider>, but need to identify collider type, eg BoxCollider

			RaycastHit hit = new RaycastHit ();
			if (part.collider.Raycast(r, out hit, CompoundPartMaxLength)) {
				collisionPoint = hit.point;
				Log.Debug ("Collider hit: " + collisionPoint.ToString (VectorFormat));
				return true;
			} else {
				collisionPoint = origin;
				Log.Debug ("Collider miss");
				return false;
			}
		}
	}
}

