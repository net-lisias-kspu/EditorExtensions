using System;
using UnityEngine;
using KSP.IO;
using System.Reflection;
using System.IO;

namespace EditorExtensions
{
	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
	public class EditorExtensions : MonoBehaviour
	{
		#region member vars

		//current vars

		//KeyCode keyMapResetCamera = KeyCode.Space;
		//KeyCode keyMapSurfaceAttachment = KeyCode.T;
		//look into loading game keymaps for applying alt+shift modifiers
		const string degreesSymbol = "\u00B0";
		int _symmetryMode = 0;
		//int maxSymmetryMode = 99;
		//static float[] angleSnapValues = { 0.0f, 1.0f, 5.0f, 15.0f, 22.5f, 30.0f, 45.0f, 60.0f, 90.0f };

		//old vars
		//const string launchSiteName_LaunchPad = "LaunchPad";
		//const string launchSiteName_Runway = "Runway";
		//bool ignoreHotKeys = false;
		//bool inVAB = false;
	
		EditorLogic editor;

		Version pluginVersion;

		ConfigData cfg;
		string pluginDirectory;
		string configFilePath;
		const string ConfigFileName = "config.xml";

		#endregion


		//bool abort = false;
		/// <summary>
		/// ctor
		/// </summary>
		public EditorExtensions ()
		{
			try {
				Assembly execAssembly = Assembly.GetExecutingAssembly ();

				pluginVersion = execAssembly.GetName ().Version;
				pluginDirectory = Path.GetDirectoryName (execAssembly.Location);

				configFilePath = Path.Combine (pluginDirectory, ConfigFileName);

				if (ModConfig.FileExists (configFilePath)) {

					cfg = LoadConfig ();

					if (cfg == null) {
						//failed to load config, create new
						cfg = CreateDefaultConfig ();
					} else {
						//check config file version
						Version fileVersion = new Version ();

						if (cfg.FileVersion != null) {
							Log.Debug ("Config v" + cfg.FileVersion + " Mod v" + pluginVersion.ToString ());

							try {
								fileVersion = new Version (cfg.FileVersion);
							} catch (Exception ex) {
								Log.Error ("Error parsing version from config file: " + ex.Message);
							}
						}

#if DEBUG
						//for debug, replace if version isn't exactly the same
						bool versionMismatch = (cfg.FileVersion == null || fileVersion != pluginVersion);
#else
						//replace if x.x doesn't match
						bool versionMismatch = (cfg.FileVersion == null || fileVersion.Major < pluginVersion.Major || (fileVersion.Major == pluginVersion.Major && fileVersion.Minor < pluginVersion.Minor));
#endif

						if (versionMismatch) {
							Log.Info ("Config file version mismatch, replacing with new defaults");
							cfg = CreateDefaultConfig ();
						} else {
							Log.Debug ("Config file is current");
						}
					}

				} else {
					cfg = CreateDefaultConfig ();
					Log.Info ("No existing config found, created new default config");
				}

				Log.Debug ("Initializing version " + pluginVersion.ToString ());
			} catch (Exception ex) {
				//abort = true;
				Log.Debug ("FATAL ERROR - Unable to initialize: " + ex.Message);
				return;
			}
		}

		private ConfigData LoadConfig ()
		{
			return ModConfig.LoadConfig (configFilePath);
		}

		private ConfigData CreateDefaultConfig ()
		{
			try {
				Log.Debug ("configFilePath: " + configFilePath);

				ConfigData defaultConfig = new ConfigData () {
					AngleSnapValues = new float[]{ 0.0f, 1.0f, 5.0f, 15.0f, 22.5f, 30.0f, 45.0f, 60.0f, 90.0f },
					MaxSymmetry = 99,
					FileVersion = pluginVersion.ToString ()
				};

				KeyMaps defaultKeys = new KeyMaps () {
					AngleSnap = KeyCode.C,
					AttachmentMode = KeyCode.T,
					PartClipping = KeyCode.Z,
					ResetCamera = KeyCode.Space,
					Symmetry = KeyCode.X
				};

				defaultConfig.KeyMap = defaultKeys;
				ModConfig.SaveConfig (defaultConfig, configFilePath);
				Log.Debug ("Created default config");
				return defaultConfig;
			} catch (Exception ex) {
				Log.Debug ("Error defaulting config: " + ex.Message);
				return null;
			}
		}
	
		//Unity initialization call
		public void Awake ()
		{
			//Log.Debug ("Awake() initializing");

			//get current editor instance
			editor = EditorLogic.fetch;
	
			//hide snap sprites
			//editor.symmetrySprite.Hide (true);
			//editor.mirrorSprite.Hide (true);
	
			InitStyles ();
	
			//Disable shortcut keys when ship name textarea has focus
		}

		//Broken
		const string VABGameObjectName = "interior_vehicleassembly";
		const string SPHGameObjectName = "xport_sph3";

		/// <summary>
		/// embiggen the hangar space
		/// currently broken
		/// </summary>
		/// <param name="editor">Editor.</param>
		void AlterEditorSpace (EditorLogic editor)
		{	
			// Modify cameras/available interior space
			if (HighLogic.LoadedScene == GameScenes.EDITOR) {
				Log.Debug ("Updating VAB dimensions and camera");
	
				VABCamera VABcam = Camera.main.GetComponent<VABCamera> ();
				VABcam.maxHeight = 2000;
				VABcam.maxDistance = 2000;
	
				GameObject interior = GameObject.Find (VABGameObjectName);
				interior.transform.localScale = new Vector3 (2.2f, 1.8f, 1.8f);
				interior.transform.position = new Vector3 (59f, 51.5f, 12);
			}
//			else if (HighLogic.LoadedScene == GameScenes.SPH)
//			{
//				Log.Debug ("Updating SPH dimensions and camera");
//	
//				SPHCamera SPHcam = Camera.main.GetComponent<SPHCamera>();
//				SPHcam.maxHeight = 2000;
//				SPHcam.maxDistance = 2000;
//				SPHcam.maxDisplaceX = 2000;
//				SPHcam.maxDisplaceZ = 2000;
//	
//				GameObject interior = GameObject.Find(SPHGameObjectName);
//				interior.transform.localScale = new Vector3(12, 6, 12);
//				interior.transform.position = new Vector3(-24.9f, -0.3f, 22.8f);
//			}
		}

		bool altKeyDown;
		bool shiftKeyDown;

		/// <summary>
		/// Fired by Unity event loop
		/// </summary>
		public void Update ()
		{		
			//if(ignoreHotKeys || editor.editorScreen != EditorLogic.EditorScreen.Parts)
			//    return;
	
			//may need to go away from this and do explicit editor.editorType calls 
			//inVAB = (editor.editorType == EditorLogic.EditorMode.VAB);

			//look into fuel crossfeed toggle
	
			//check for the various alt/mod etc keypresses
			altKeyDown = Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt) || Input.GetKey (KeyCode.AltGr);
			//check for shift key
			shiftKeyDown = Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift);

			//look into skewing camera
	
			//Space - when no part is selected, reset camera
			if (Input.GetKeyDown (cfg.KeyMap.ResetCamera) && !EditorLogic.SelectedPart) {
				//if (HighLogic.LoadedSceneIsEditor) {
				VABCamera VABcam = Camera.main.GetComponent<VABCamera> ();
				VABcam.camPitch = 0;
				VABcam.camHdg = 0;
				//VABcam.ResetCamera ();

				SPHCamera SPHcam = Camera.main.GetComponent<SPHCamera> ();
				SPHcam.camPitch = 0;
				SPHcam.camHdg = 0;
				//SPHcam.ResetCamera();
				//}
			}
	
			//Broken, api doesnt respond
			// V - Vertical alignment toggle
//			if (Input.GetKeyDown (KeyCode.V)) {
//				//Log.Debug ("Toggling vertical snap");
//				GameSettings.VAB_ANGLE_SNAP_INCLUDE_VERTICAL ^= true;
//	
//				//if normal radial angle snap is currently off, vertical snap will have no effect unless it is re-enabled
//				//automatically set aangle snap to minimum - some people thought vert snap was broken in this situation, the game doesn't appear to allow it
//				if (GameSettings.VAB_USE_ANGLE_SNAP == false && GameSettings.VAB_ANGLE_SNAP_INCLUDE_VERTICAL == true) {
//					Log.Debug ("Enabling angle snap to allow vertical snap to work");
//					//angle snap needs be > 0, otherwise log is spammed with DivideByZero errors
//					if (editor.srfAttachAngleSnap == 0)
//						editor.srfAttachAngleSnap = 1;
//					GameSettings.VAB_USE_ANGLE_SNAP = true;
//				}
//				OSDMessage ("Vertical snap " + (GameSettings.VAB_ANGLE_SNAP_INCLUDE_VERTICAL ? "enabled" : "disabled"), 1);
//				Log.Debug ("Vertical snap " + (GameSettings.VAB_ANGLE_SNAP_INCLUDE_VERTICAL ? "enabled" : "disabled"));
//				return;
//			}
	
			//look into also toggling  editor.allowNodeAttachment
			//EditorLogic.SelectedPart.attachRules.allowStack

			// T: Surface attachment and node attachment toggle
			if (Input.GetKeyDown (cfg.KeyMap.AttachmentMode)) {

				Part selectedPart = EditorLogic.SelectedPart;

				if (selectedPart) {
					//Toggle surface attachment for selected part
					selectedPart.attachRules.srfAttach ^= true;

					//set global toggling to match
					editor.allowSrfAttachment = selectedPart.attachRules.srfAttach;
					editor.allowNodeAttachment = !selectedPart.attachRules.srfAttach;

					Log.Debug ("Toggling srfAttach for " + EditorLogic.SelectedPart.name);
					OSDMessage (String.Format ("Surface attachment {0} \n Node attachment {1} \n for {2}"
						, selectedPart.attachRules.srfAttach ? "enabled" : "disabled"
						, editor.allowNodeAttachment ? "enabled" : "disabled"
						, selectedPart.name
					), 1);
				} else {
					//just toggle global surface attachment, parts whose config do not allow it are unaffected
					editor.allowSrfAttachment ^= true;
					editor.allowNodeAttachment = !editor.allowSrfAttachment;
					OSDMessage (String.Format ("Surface attachment {0} \n Node attachment {1}"
						, editor.allowSrfAttachment ? "enabled" : "disabled"
						, editor.allowNodeAttachment ? "enabled" : "disabled"
					), 1);
				}
			}
	
			// ALT+Z : Toggle part clipping (From cheat options)
			if (altKeyDown && Input.GetKeyDown (cfg.KeyMap.PartClipping)) {
				CheatOptions.AllowPartClipping ^= true;
				Log.Debug ("AllowPartClipping " + (CheatOptions.AllowPartClipping ? "enabled" : "disabled"));
				OSDMessage ("Part clipping " + (CheatOptions.AllowPartClipping ? "enabled" : "disabled"), 1);
				return;
			}
	
			// C, Shift+C : Increment/Decrement Angle snap
			if (Input.GetKeyDown (cfg.KeyMap.AngleSnap)) {
	
				if (!altKeyDown) {
					Log.Debug ("Starting srfAttachAngleSnap = " + editor.srfAttachAngleSnap.ToString ());
	
					int currentAngleIndex = Array.IndexOf (cfg.AngleSnapValues, editor.srfAttachAngleSnap);
	
					Log.Debug ("currentAngleIndex: " + currentAngleIndex.ToString ());
	
					//rotate through the angle snap values
					float newAngle;
					if (shiftKeyDown) {
						//lower snap
						newAngle = cfg.AngleSnapValues [currentAngleIndex == 0 ? cfg.AngleSnapValues.Length - 1 : currentAngleIndex - 1];
					} else {
						//higher snap
						//Log.Debug ("new AngleIndex: " + (currentAngleIndex == angleSnapValues.Length - 1 ? 0 : currentAngleIndex + 1).ToString ());
						newAngle = cfg.AngleSnapValues [currentAngleIndex == cfg.AngleSnapValues.Length - 1 ? 0 : currentAngleIndex + 1];
					}
	
					Log.Debug ("Setting srfAttachAngleSnap to " + newAngle.ToString ());
					editor.srfAttachAngleSnap = newAngle;
				} else {
					Log.Debug ("Resetting srfAttachAngleSnap to 0");
					editor.srfAttachAngleSnap = 0;
				}
	
				//at angle snap 0, turn off angle snap and show stock circle sprite
				if (editor.srfAttachAngleSnap == 0) {
					GameSettings.VAB_USE_ANGLE_SNAP = false;
					//set playanim index and unhide stock sprite
					//editor.angleSnapSprite.PlayAnim (0);
					//editor.angleSnapSprite.Hide (false);
				} else {
					GameSettings.VAB_USE_ANGLE_SNAP = true;
					//angle snap is on, re-hide stock sprite
					//editor.angleSnapSprite.Hide (true);
				}
	
				Log.Debug ("Exiting srfAttachAngleSnap = " + editor.srfAttachAngleSnap.ToString ());
				return;
	
			}
	
			// X, Shift+X : Increment/decrement symmetry mode
			if (Input.GetKeyDown (cfg.KeyMap.Symmetry)) {

				//only inc/dec symmetry in radial mode, mirror is just 1&2
				if (editor.symmetryMethod == SymmetryMethod.Radial) {
					if (altKeyDown || (_symmetryMode < 2 && shiftKeyDown)) {
						//Alt+X or Symmetry is at 1(index 2) or lower
						_symmetryMode = 0;
					} else if (_symmetryMode > cfg.MaxSymmetry - 2 && !shiftKeyDown) {
						//Stop adding at max symmetry
						_symmetryMode = cfg.MaxSymmetry - 1;
					} else {
						//inc/dec symmetry
						_symmetryMode = _symmetryMode + (shiftKeyDown ? -1 : 1);
					}
					editor.symmetryMode = _symmetryMode;
					Log.Debug ("Setting symmetry to " + _symmetryMode.ToString ());
				} else {
					//editor.symmetryMethod == SymmetryMethod.Mirror
					//update var with stock action's result
					_symmetryMode = editor.symmetryMode;
				}
			}
		}

		#region GUI

		GUIStyle osdLabelStyle, labelStyle;

		/// <summary>
		/// Init styles for GUI items
		/// </summary>
		void InitStyles ()
		{
			//Log.Debug ("InitStyles()");	
	
			osdLabelStyle = new GUIStyle ();
			osdLabelStyle.stretchWidth = true;
			osdLabelStyle.alignment = TextAnchor.MiddleCenter;
			osdLabelStyle.fontSize = 24;
			osdLabelStyle.fontStyle = FontStyle.Bold;
			osdLabelStyle.normal.textColor = Color.yellow;
	
			labelStyle = new GUIStyle ("Label");
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.fontSize = 18;
			//labelStyle.fontStyle = FontStyle.Bold;
			labelStyle.normal.textColor = XKCDColors.DarkYellow;
		}

		/// <summary>
		/// Unity GUI paint event, fired every screen refresh
		/// </summary>
		public void OnGUI ()
		{	
			//show on-screen messages
			DisplayOSD ();

			//show and update the angle snap and symmetry mode labels
			ShowSnapLabels ();
		}

		float messageCutoff = 0;
		string messageText = "";

		/// <summary>
		/// Set a on screen display message
		/// </summary>
		/// <param name="message">Message string</param>
		/// <param name="delay">Amount of time to display the message</param>
		void OSDMessage (string message, float delay)
		{
			messageCutoff = Time.time + delay;
			messageText = message;
			Log.Debug (String.Format ("OSD messageCutoff = {0}, messageText = {1}", messageCutoff.ToString (), messageText));
		}

		/// <summary>
		/// check for On Screen Display message
		/// </summary>
		void DisplayOSD ()
		{
			if (Time.time < messageCutoff) {
				GUILayout.BeginArea (new Rect (0, (Screen.height / 4), Screen.width, 200), osdLabelStyle);
				GUILayout.Label (messageText, osdLabelStyle);
				GUILayout.EndArea ();			
			}
		}

		string symmetryLabelValue = string.Empty;

		//symmetry & angle sprite/label size and position
		const int advancedModeOffset = 34;
		const int angleSnapLabelSize = 46;
		const int angleSnapLabelLeftOffset = 207;
		const int angleSnapLabelBottomOffset = 63;
		const int symmetryLabelSize = 57;
		const int symmetryLabelLeftOffset = 150;
		const int symmetryLabelBottomOffset = 65;
		Rect angleSnapLabelRect = new Rect () {
			xMin = angleSnapLabelLeftOffset,
			xMax = angleSnapLabelLeftOffset + angleSnapLabelSize,
			yMin = Screen.height - angleSnapLabelBottomOffset,
			yMax = Screen.height - angleSnapLabelBottomOffset + angleSnapLabelSize
		};
		Rect symmetryLabelRect = new Rect () {
			xMin = symmetryLabelLeftOffset,
			xMax = symmetryLabelLeftOffset + symmetryLabelSize,
			yMin = Screen.height - symmetryLabelBottomOffset,
			yMax = Screen.height - symmetryLabelBottomOffset + symmetryLabelSize
		};

		/// <summary>
		/// Hides the stock angle & symmetry sprites and replaces with textual labels
		/// </summary>
		private void ShowSnapLabels ()
		{
			//Only show angle/symmetry sprites on parts tab
			if (editor.editorScreen == EditorScreen.Parts) {
				if (EditorLogic.Mode == EditorLogic.EditorModes.ADVANCED) {
					//in advanced mode, shift labels to the right
					angleSnapLabelRect.xMin = angleSnapLabelLeftOffset + advancedModeOffset;
					angleSnapLabelRect.xMax = angleSnapLabelLeftOffset + angleSnapLabelSize + advancedModeOffset;
					symmetryLabelRect.xMin = symmetryLabelLeftOffset + advancedModeOffset;
					symmetryLabelRect.xMax = symmetryLabelLeftOffset + symmetryLabelSize + advancedModeOffset;
				} else {
					//EditorLogic.EditorModes.SIMPLE
					//in simple mode, set back to left position
					angleSnapLabelRect.xMin = angleSnapLabelLeftOffset;
					angleSnapLabelRect.xMax = angleSnapLabelLeftOffset + angleSnapLabelSize;
					symmetryLabelRect.xMin = symmetryLabelLeftOffset;
					symmetryLabelRect.xMax = symmetryLabelLeftOffset + symmetryLabelSize;
				}



				//Radial mode 'number+R', mirror mode is 'M'/'MM'
				if (editor.symmetryMethod == SymmetryMethod.Radial) {
					symmetryLabelValue = (editor.symmetryMode + 1) + "R";
				} else if (editor.symmetryMethod == SymmetryMethod.Mirror) {
					symmetryLabelValue = (editor.symmetryMode == 0) ? "M" : "MM";
				}

				//always hide stock symmetry and mirror sprites
				editor.symmetrySprite.Hide (true);
				editor.mirrorSprite.Hide (true);

				// Show Symmetry label
				GUI.Label (symmetryLabelRect, symmetryLabelValue, labelStyle);

				//if angle snap is on hide stock sprite
				if (GameSettings.VAB_USE_ANGLE_SNAP) {
					editor.angleSnapSprite.Hide (true);
					GUI.Label (angleSnapLabelRect, editor.srfAttachAngleSnap + degreesSymbol, labelStyle);

				} else {
					//angle snap is off, show stock sprite
					editor.angleSnapSprite.PlayAnim (0);
					editor.angleSnapSprite.Hide (false);
				}
			}
		}

		#endregion
	}
}
