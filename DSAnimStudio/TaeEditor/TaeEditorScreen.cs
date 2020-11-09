﻿using DSAnimStudio.GFXShaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SoulsFormats;
using SoulsAssetPipeline.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static DSAnimStudio.TaeEditor.TaeEditAnimEventGraph;
using System.Diagnostics;
using SharpDX.DirectWrite;
using System.IO;
using System.Threading.Tasks;
using DSAnimStudio.ImguiOSD;

namespace DSAnimStudio.TaeEditor
{
    public class TaeEditorScreen
    {

        public const string BackupExtension = ".dsasbak";

        private ContentManager DebugReloadContentManager = null;

        public void Tools_ScanForUnusedAnimations()
        {
            List<string> usedAnims = new List<string>();
            List<string> unusedAnims = new List<string>();
            foreach (var anim in SelectedTae.Animations)
            {
                string hkx = Graph.ViewportInteractor.GetFinalAnimFileName(SelectedTae, anim);
                if (!usedAnims.Contains(hkx))
                    usedAnims.Add(hkx);
            }
            foreach (var anim in Graph.ViewportInteractor.CurrentModel.AnimContainer.Animations.Keys)
            {
                if (!usedAnims.Contains(anim) && !Graph.ViewportInteractor.CurrentModel.AnimContainer.AdditiveBlendOverlayNames.Contains(anim)
                && !unusedAnims.Contains(anim))
                    unusedAnims.Add(anim);
            }
            //var sb = new StringBuilder();
            foreach (var anim in unusedAnims)
            {
                //sb.AppendLine(anim);
                int id = int.Parse(anim.Replace(".hkx", "").Replace("_", "").Replace("a", ""));
                var newAnim = new TAE.Animation(9_000_000000 + id, new TAE.Animation.AnimMiniHeader.Standard()
                {
                    ImportHKXSourceAnimID = id,
                    ImportsHKX = true,
                }, $"UNUSED:{anim.Replace(".hkx", "")}");
                SelectedTae.Animations.Add(newAnim);
            }
            RecreateAnimList();
        }

        public void Tools_DowngradeSekiroAnibnds()
        {
            Main.WinForm.Invoke(new Action(() =>
            {
                var browseDlg = new System.Windows.Forms.OpenFileDialog()
                {
                    Filter = "*.ANIBND.DCX|*.ANIBND.DCX|*.REMOBND.DCX|*.REMOBND.DCX",
                    ValidateNames = true,
                    CheckFileExists = true,
                    CheckPathExists = true,
                    //ShowReadOnly = true,
                    Title = "Choose *.ANIBND.DCX file(s) to downgrade to Havok 2010.",
                    Multiselect = true,
                };

                var decision = browseDlg.ShowDialog();

                if (decision == System.Windows.Forms.DialogResult.OK)
                {
                    //if (System.IO.File.Exists(browseDlg.FileName + ".2010"))
                    //{
                    //    var nextDecision = System.Windows.Forms.MessageBox.Show("A '*.anibnd.dcx.2010' version of the selected file already exists. Would you like to downgrade all animations and overwrite this file anyways?", "Convert Again?", System.Windows.Forms.MessageBoxButtons.YesNo);

                    //    if (nextDecision == System.Windows.Forms.DialogResult.No)
                    //        return;
                    //}

                    LoadingTaskMan.DoLoadingTask("PreprocessSekiroAnimations_Multi", "Downgrading all selected ANIBNDs...", prog =>
                    {
                        int numNames = browseDlg.FileNames.Length;
                        for (int i = 0; i < numNames; i++)
                        {
                            string curName = browseDlg.FileNames[i];

                            LoadingTaskMan.DoLoadingTaskSynchronous($"PreprocessSekiroAnimations_{curName}", $"Downgrading {Utils.GetShortIngameFileName(curName)}...", prog2 =>
                            {

                                try
                                {

                                    string debug_testConvert_filename = curName;

                                    if (string.IsNullOrWhiteSpace(debug_testConvert_filename))
                                        throw new Exception("WHAT THE FUCKING FUCK GOD FUCKING DAMMIT");

                                    string folder = new System.IO.FileInfo(debug_testConvert_filename).DirectoryName;

                                    int lastSlashInFolder = folder.LastIndexOf("\\");

                                    string interroot = folder.Substring(0, lastSlashInFolder);

                                    string oodleSource = Utils.Frankenpath(interroot, "oo2core_6_win64.dll");

                                    string oodleTarget = Utils.Frankenpath(Main.Directory, "oo2core_6_win64.dll");

                                    // modengine check
                                    if (!File.Exists(oodleSource))
                                    {
                                        oodleSource = Utils.Frankenpath(interroot, @"..\oo2core_6_win64.dll");
                                    }

                                    if (System.IO.File.Exists(oodleSource) && !System.IO.File.Exists(oodleTarget))
                                    {
                                        System.IO.File.Copy(oodleSource, oodleTarget, true);

                                        System.Windows.Forms.MessageBox.Show("Oodle compression library was automatically copied from game directory " +
                                            "to editor's '/lib' directory and Sekiro files will load.\n\n", "Required Library Copied",
                                            System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                                    }


                                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();


                                    HavokDowngrade.DowngradeAnibnd(debug_testConvert_filename, prog2);
                                    stopwatch.Stop();
                                    int minutes = (int)(stopwatch.Elapsed.TotalSeconds / 60);
                                    int seconds = (int)(Math.Round(stopwatch.Elapsed.TotalSeconds % 60));

                                    //if (minutes > 0)
                                    //    System.Windows.Forms.MessageBox.Show($"Created downgraded file (\"*.anibnd.dcx.2010\").\nElapsed Time: {minutes}m{seconds}s", "Animation Downgrading Complete");
                                    //else
                                    //    System.Windows.Forms.MessageBox.Show($"Created downgraded file (\"*.anibnd.dcx.2010\").\nElapsed Time: {seconds}s", "Animation Downgrading Complete");

                                }
                                catch (DllNotFoundException)
                                {
                                    System.Windows.Forms.MessageBox.Show("Was unable to automatically find the " +
                                        "`oo2core_6_win64.dll` file in the Sekiro folder. Please copy that file to the " +
                                        "'lib' folder next to DS Anim Studio.exe in order to load Sekiro files.", "Unable to find compression DLL",
                                        System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                                }

                            });

                            prog.Report(1.0 * (i + 1) / browseDlg.FileNames.Length);
                            //LoadingTaskMan.DoLoadingTaskSynchronous($"PreprocessSekiroAnimations_Multi_{i}",
                            //    $"Downgrading anim {(i + 1)} of {browseDlg.FileNames.Length}...", prog2 =>
                            //    {
                            //        DoAnimDowngrade(browseDlg.FileNames[i]);
                            //        prog.Report(1.0 * (i + 1) / browseDlg.FileNames.Length);

                            //    });
                        }
                    });



                }

            }));
        }

        public void Tools_ImportAllPTDEAnibndToDS1R()
        {
            var browseDlgPTDE = new System.Windows.Forms.OpenFileDialog()
            {
                Filter = "*.EXE|*.EXE",
                ValidateNames = true,
                CheckFileExists = true,
                CheckPathExists = true,
                //ShowReadOnly = true,
                Title = "Select your PTDE DARKSOULS.EXE...",
                Multiselect = false,
            };

            var browseDlgDS1R = new System.Windows.Forms.OpenFileDialog()
            {
                Filter = "*.EXE|*.EXE",
                ValidateNames = true,
                CheckFileExists = true,
                CheckPathExists = true,
                //ShowReadOnly = true,
                Title = "Select your DS1R DarkSoulsRemastered.EXE...",
                Multiselect = false,
            };

            if (browseDlgPTDE.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (browseDlgDS1R.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string folderPTDE = Utils.Frankenpath(new System.IO.FileInfo(browseDlgPTDE.FileName).DirectoryName, "chr\\");
                    string folderDS1R = Utils.Frankenpath(new System.IO.FileInfo(browseDlgDS1R.FileName).DirectoryName, "chr\\");

                    foreach (var file in System.IO.Directory.GetFiles(folderPTDE, "*.anibnd"))
                    {
                        string nickname = Utils.GetShortIngameFileName(file);
                        System.IO.File.Copy(file, Utils.Frankenpath(folderDS1R, $"{nickname}.anibnd.dcx.2010"), true);
                    }

                    System.Windows.Forms.MessageBox.Show("Imported all from PTDE.");
                }
            }
        }

        public void Tools_ExportCurrentTAE()
        {
            Main.WinForm.Invoke(new Action(() =>
            {
                var browseDlg = new System.Windows.Forms.SaveFileDialog()
                {
                    Filter = "TAE Files (*.tae)|*.tae",
                    ValidateNames = true,
                    CheckPathExists = true,
                    //ShowReadOnly = true,
                    Title = "Choose where to save loose TAE file.",

                };

                var decision = browseDlg.ShowDialog();

                if (decision == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        SelectedTae.Write(browseDlg.FileName);
                        System.Windows.Forms.MessageBox.Show("TAE saved successfully.", "Saved");
                    }
                    catch (Exception exc)
                    {
                        System.Windows.Forms.MessageBox.Show($"Error saving TAE file:\n\n{exc}", "Failed to Save",
                            System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    }
                }

            }));
        }

        public void BringUpImporter_FLVER2()
        {
            if (ImporterWindow_FLVER2 == null || ImporterWindow_FLVER2.IsDisposed || !ImporterWindow_FLVER2.Visible)
            {
                ImporterWindow_FLVER2?.Dispose();
                ImporterWindow_FLVER2 = null;
            }
            ImporterWindow_FLVER2 = new SapImportFlver2Form();
            ImporterWindow_FLVER2.ImportConfig = Config.LastUsedImportConfig_FLVER2;
            ImporterWindow_FLVER2.MainScreen = this;
            ImporterWindow_FLVER2.Show();
            // The CenterParent stuff just doesn't work for some reason, heck it.

            Main.CenterForm(ImporterWindow_FLVER2);

            ImporterWindow_FLVER2.LoadValuesFromConfig();
        }

        public void ShowComboMenu()
        {
            GameWindowAsForm.Invoke(new Action(() =>
            {
                if (!IsFileOpen)
                    return;

                if (ComboMenu == null || ComboMenu.IsDisposed)
                {
                    ComboMenu = new TaeComboMenu();
                    ComboMenu.Owner = GameWindowAsForm;
                    ComboMenu.MainScreen = this;
                    ComboMenu.SetupTaeComboBoxes();
                }

                ComboMenu.Show();
                Main.CenterForm(ComboMenu);
                ComboMenu.Activate();
            }));
            
        }

        public int IsDelayLoadConfig = 30;

        public TaeComboMenu ComboMenu = null;

        public float AnimSwitchRenderCooldown = 0;
        public float AnimSwitchRenderCooldownMax = 0.3f;
        public float AnimSwitchRenderCooldownFadeLength = 0.1f;
        public Color AnimSwitchRenderCooldownColor = Color.Black * 0.35f;

        private bool HasntSelectedAnAnimYetAfterBuildingAnimList = true;

        enum DividerDragMode
        {
            None,
            LeftVertical,
            RightVertical,
            RightPaneHorizontal,
        }

        public enum ScreenMouseHoverKind
        {
            None,
            AnimList,
            EventGraph,
            Inspector,
            ModelViewer,
            DividerBetweenCenterAndLeftPane,
            DividerBetweenCenterAndRightPane,
            DividerRightPaneHorizontal,
            ShaderAdjuster
        }

        public DbgMenus.DbgMenuPadRepeater NextAnimRepeaterButton = new DbgMenus.DbgMenuPadRepeater(Buttons.DPadDown, 0.4f, 0.016666667f);
        public DbgMenus.DbgMenuPadRepeater PrevAnimRepeaterButton = new DbgMenus.DbgMenuPadRepeater(Buttons.DPadUp, 0.4f, 0.016666667f);

        public DbgMenus.DbgMenuPadRepeater NextFrameRepeaterButton = new DbgMenus.DbgMenuPadRepeater(Buttons.DPadLeft, 0.3f, 0.016666667f);
        public DbgMenus.DbgMenuPadRepeater PrevFrameRepeaterButton = new DbgMenus.DbgMenuPadRepeater(Buttons.DPadRight, 0.3f, 0.016666667f);

        public static bool CurrentlyEditingSomethingInInspector;
        
        public class FindInfoKeep
        {
            public enum TaeSearchType : int
            {
                ParameterValue = 0,
                ParameterName = 1,
                EventName = 2,
                EventType = 3,
            }

            public string SearchQuery;
            public bool MatchEntireString;
            public List<TaeFindResult> Results;
            public int HighlightedIndex;
            public TaeSearchType SearchType;
        }

        public SapImportFlver2Form ImporterWindow_FLVER2 = null;

        public FindInfoKeep LastFindInfo = null;
        public TaeFindValueDialog FindValueDialog = null;

        public TaePlaybackCursor PlaybackCursor => Graph?.PlaybackCursor;

        public Rectangle ModelViewerBounds;
        public Rectangle ModelViewerBounds_InputArea;

        private const int RECENT_FILES_MAX = 32;

        //TODO: CHECK THIS
        private int TopMenuBarMargin => 24;

        private int TopOfGraphAnimInfoMargin = 20;

        private int TransportHeight = 28;

        public TaeTransport Transport;

        public void GoToEventSource()
        {
            if (Graph.AnimRef.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim asImportOtherAnim)
            {

                var animRef = FileContainer.GetAnimRefFull(asImportOtherAnim.ImportFromAnimID);

                if (animRef.Item1 == null || animRef.Item2 == null)
                {
                    DialogManager.DialogOK("Invalid Animation Reference", $"Animation ID referenced ({asImportOtherAnim.ImportFromAnimID}) does not exist.");
                    return;
                }
                SelectNewAnimRef(animRef.Item1, animRef.Item2);
            }
        }

        //public bool CtrlHeld;
        //public bool ShiftHeld;
        //public bool AltHeld;

        public const string HELP_TEXT =
            "Left Click + Drag on Timeline:\n" +
            "    Scrub animation frame.\n" +
            "Left Click + Hold Shift + Drag on Timeline:\n" +
            "    Scrub animation frame while ignoring autoscroll.\n" +
            "Left Click + Drag Middle of Event:\n" +
            "    Move whole event.\n" +
            "Left Click + Drag Left/Right Side of Event:\n" +
            "    Move start/end of event.\n" +
            "Left Click:\n" +
            "    Highlight event under mouse cursor.\n" +
            "Left Click and Drag:\n" +
            "    Drag selection rectangle to highlight multiple events.\n" +
            "Shift + Left Click:\n" +
            "    Add to current selection (works for multiselect as well).\n" +
            "Ctrl + Left Click:\n" +
            "    Subtract from current selection (works for multiselect as well).\n" +
            "Right Click:\n" +
            "    Place copy of last highlighted event at mouse cursor.\n" +
            "Delete Key:\n" +
            "    Delete highlighted event.\n" +
            "Ctrl+X/Ctrl+C/Ctrl+V:\n" +
            "    CUT/COPY/PASTE.\n" +
            "Ctrl+Z/Ctrl+Y:\n" +
            "    UNDO/REDO.\n" +
            "F1 Key:\n" +
            "    Change type of highlighted event.\n" +
            "Space Bar:\n" +
            "    Play/Pause Anim.\n" +
            "Shift+Space Bar:\n" +
            "    Play Anim from beginning.\n" +
            "R Key:\n" +
            "    Reset root motion (useful for root motion accumulation option).\n" +
            "Ctrl+Mouse Wheel:\n" +
            "    Zoom timeline in/out.\n" +
            "Ctrl+(+/-/0):\n" +
            "   Zoom in/out/reset, exactly like in web browsers.\n" +
            "Left/Right Arrow Keys:\n" +
            "    Goes to previous/next frame.\n" +
            "Home/End:\n" +
            "    Without Shift: Go to playback start point / playback end point.\n" +
            "    With Shift: Go to start of animation / end of animation.\n" +
            "    With Ctrl: Stop playing animation after jumping.\n" +
            "    (Ctrl and Shift can be combined.)\n";

        private static object _lock_PauseUpdate = new object();
        private bool _PauseUpdate;
        private bool PauseUpdate
        {
            get
            {
                lock (_lock_PauseUpdate)
                    return _PauseUpdate;
            }
            set
            {
                lock (_lock_PauseUpdate)
                    _PauseUpdate = value;
            }
        }
        //private float _PauseUpdateTotalTime;
        //private float PauseUpdateTotalTime
        //{
        //    get
        //    {
        //        lock (_lock_PauseUpdate)
        //            return _PauseUpdateTotalTime;
        //    }
        //    set
        //    {
        //        lock (_lock_PauseUpdate)
        //            _PauseUpdateTotalTime = value;
        //    }
        //}

        public Rectangle Rect;

        public Dictionary<TAE.Animation, TaeUndoMan> UndoManDictionary 
            = new Dictionary<TAE.Animation, TaeUndoMan>();

        public TaeUndoMan UndoMan
        {
            get
            {
                if (SelectedTaeAnim == null)
                    return null;

                if (!UndoManDictionary.ContainsKey(SelectedTaeAnim))
                {
                    var newUndoMan = new TaeUndoMan(this);
                    UndoManDictionary.Add(SelectedTaeAnim, newUndoMan);
                }
                return UndoManDictionary[SelectedTaeAnim];
            }
        }

        public bool IsModified
        {
            get
            {
                try
                {
                    return (SelectedTae?.Animations.Any(a => a.GetIsModified()) ?? false) ||
                    (FileContainer?.AllTAE.Any(t => t.GetIsModified()) ?? false) || (FileContainer?.IsModified ?? false);
                }
                catch
                {
                    return true;
                }
            }
        }

        private void PushNewRecentFile(string fileName)
        {
            while (Config.RecentFilesList.Contains(fileName))
                Config.RecentFilesList.Remove(fileName);

            while (Config.RecentFilesList.Count >= RECENT_FILES_MAX)
                Config.RecentFilesList.RemoveAt(Config.RecentFilesList.Count - 1);

            Config.RecentFilesList.Insert(0, fileName);

            SaveConfig();
        }


        private TaeButtonRepeater UndoButton = new TaeButtonRepeater(0.4f, 0.05f);
        private TaeButtonRepeater RedoButton = new TaeButtonRepeater(0.4f, 0.05f);

        public float LeftSectionWidth = 236;
        private const float LeftSectionWidthMin = 120;
        private float DividerLeftVisibleStartX => Rect.Left + LeftSectionWidth;
        private float DividerLeftVisibleEndX => Rect.Left + LeftSectionWidth + DividerVisiblePad;

        public float RightSectionWidth = 600;
        private const float RightSectionWidthMin = 320;
        private float DividerRightVisibleStartX => Rect.Right - RightSectionWidth - DividerVisiblePad;
        private float DividerRightVisibleEndX => Rect.Right - RightSectionWidth;


        private float DividerRightCenterX => DividerRightVisibleStartX + ((DividerRightVisibleEndX - DividerRightVisibleStartX) / 2);
        private float DividerLeftCenterX => DividerLeftVisibleStartX + ((DividerLeftVisibleEndX - DividerLeftVisibleStartX) / 2);

        private float DividerRightGrabStartX => DividerRightCenterX - (DividerHitboxPad / 2);
        private float DividerRightGrabEndX => DividerRightCenterX + (DividerHitboxPad / 2);

        private float DividerLeftGrabStartX => DividerLeftCenterX - (DividerHitboxPad / 2);
        private float DividerLeftGrabEndX => DividerLeftCenterX + (DividerHitboxPad / 2);

        public float TopRightPaneHeight = 600;
        private const float TopRightPaneHeightMinNew = 128;
        private const float BottomRightPaneHeightMinNew = 64;

        private float DividerRightPaneHorizontalVisibleStartY => Rect.Top + TopRightPaneHeight + TopMenuBarMargin + TransportHeight;
        private float DividerRightPaneHorizontalVisibleEndY => Rect.Top + TopRightPaneHeight + DividerVisiblePad + TopMenuBarMargin + TransportHeight;
        private float DividerRightPaneHorizontalCenterY => DividerRightPaneHorizontalVisibleStartY + ((DividerRightPaneHorizontalVisibleEndY - DividerRightPaneHorizontalVisibleStartY) / 2);

        private float DividerRightPaneHorizontalGrabStartY => DividerRightPaneHorizontalCenterY - (DividerHitboxPad / 2);
        private float DividerRightPaneHorizontalGrabEndY => DividerRightPaneHorizontalCenterY + (DividerHitboxPad / 2);

        private float LeftSectionStartX => Rect.Left;
        private float MiddleSectionStartX => DividerLeftVisibleEndX;
        private float RightSectionStartX => Rect.Right - RightSectionWidth;

        private float MiddleSectionWidth => DividerRightVisibleStartX - DividerLeftVisibleEndX;
        private const float MiddleSectionWidthMin = 128;

        private float DividerVisiblePad = 3;
        private int DividerHitboxPad = 10;

        private DividerDragMode CurrentDividerDragMode = DividerDragMode.None;

        public ScreenMouseHoverKind MouseHoverKind = ScreenMouseHoverKind.None;
        private ScreenMouseHoverKind oldMouseHoverKind = ScreenMouseHoverKind.None;
        public ScreenMouseHoverKind WhereCurrentMouseClickStarted = ScreenMouseHoverKind.None;

        public TaeFileContainer FileContainer;

        public bool IsFileOpen => FileContainer != null;

        public TAE SelectedTae { get; private set; }

        public TAE.Animation SelectedTaeAnim { get; private set; }
        private TaeScrollingString SelectedTaeAnimInfoScrollingText = new TaeScrollingString();

        public readonly System.Windows.Forms.Form GameWindowAsForm;

        public bool QueuedChangeEventType = false;

        public bool SingleEventBoxSelected => SelectedEventBox != null && MultiSelectedEventBoxes.Count == 0;

        public TaeEditAnimEventBox PrevHoveringOverEventBox = null;

        public TaeEditAnimEventBox HoveringOverEventBox = null;

        private TaeEditAnimEventBox _selectedEventBox = null;
        public TaeEditAnimEventBox SelectedEventBox
        {
            get => _selectedEventBox;
            set
            {
                //inspectorWinFormsControl.DumpDataGridValuesToEvent();

                //if (value != null && value != _selectedEventBox)
                //{
                //    if (Config.UseGamesMenuSounds)
                //        FmodManager.PlaySE("f000000000");
                //}

                _selectedEventBox = value;

                if (_selectedEventBox == null)
                {
                    //inspectorWinFormsControl.buttonChangeType.Enabled = false;
                }
                else
                {
                    //inspectorWinFormsControl.buttonChangeType.Enabled = true;

                    // If one box was just selected, clear the multi-select
                    MultiSelectedEventBoxes.Clear();
                }
            }
        }

        public List<TaeEditAnimEventBox> MultiSelectedEventBoxes = new List<TaeEditAnimEventBox>();
        private int multiSelectedEventBoxesCountLastFrame = 0;

        public TaeEditAnimList AnimationListScreen;
        public TaeEditAnimEventGraph Graph { get; private set; }
        public bool IsCurrentlyLoadingGraph { get; private set; } = false;
        //private Dictionary<TAE.Animation, TaeEditAnimEventGraph> GraphLookup = new Dictionary<TAE.Animation, TaeEditAnimEventGraph>();
        //private TaeEditAnimEventGraphInspector editScreenGraphInspector;

        private Color ColorInspectorBG = Color.DarkGray;
        public System.Numerics.Vector2 ImGuiEventInspectorPos = System.Numerics.Vector2.Zero;
        public System.Numerics.Vector2 ImGuiEventInspectorSize = System.Numerics.Vector2.Zero;

        public FancyInputHandler Input => Main.Input;

        public string FileContainerName = "";
        public string FileContainerName_2010 => FileContainerName + ".2010";

        public bool IsReadOnlyFileMode = false;

        public TaeConfigFile Config = new TaeConfigFile();

        private static string ConfigFilePath = null;

        private static void CheckConfigFilePath()
        {
            if (ConfigFilePath == null)
            {
                var currentAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var currentAssemblyDir = System.IO.Path.GetDirectoryName(currentAssemblyPath);
                ConfigFilePath = System.IO.Path.Combine(currentAssemblyDir, "DSAnimStudio_Config.json");
            }
        }

        public void LoadConfig()
        {
            CheckConfigFilePath();
            if (!System.IO.File.Exists(ConfigFilePath))
            {
                Config = new TaeConfigFile();
                SaveConfig();
            }

            var jsonText = System.IO.File.ReadAllText(ConfigFilePath);

            Config = Newtonsoft.Json.JsonConvert.DeserializeObject<TaeConfigFile>(jsonText);

            Config.AfterLoading(this);
        }

        public void SaveConfig()
        {
            if (Graph != null)
            {
                // I'm sorry; this is pecularily placed.
                Graph.ViewportInteractor.SaveChrAsm();
            }

            Config.BeforeSaving(this);

            CheckConfigFilePath();

            var jsonText = Newtonsoft.Json.JsonConvert
                .SerializeObject(Config,
                Newtonsoft.Json.Formatting.Indented);

            System.IO.File.WriteAllText(ConfigFilePath, jsonText);
        }

        public bool? LoadCurrentFile()
        {
            IsCurrentlyLoadingGraph = true;
            Scene.DisableModelDrawing();
            Scene.DisableModelDrawing2();

            // Even if it fails to load, just always push it to the recent files list
            PushNewRecentFile(FileContainerName);

            //string templateName = BrowseForXMLTemplate();

            //if (templateName == null)
            //{
            //    return false;
            //}

            if (System.IO.File.Exists(FileContainerName))
            {
                FileContainer = new TaeFileContainer();

                try
                {
                    string folder = new System.IO.FileInfo(FileContainerName).DirectoryName;

                    int lastSlashInFolder = folder.LastIndexOf("\\");

                    string interroot = folder.Substring(0, lastSlashInFolder);

                    string oodleSource = Utils.Frankenpath(interroot, "oo2core_6_win64.dll");

                    // modengine check
                    if (!File.Exists(oodleSource))
                    {
                        oodleSource = Utils.Frankenpath(interroot, @"..\oo2core_6_win64.dll");
                    }

                    //if (!File.Exists(oodleSource))
                    //{
                    //    System.Windows.Forms.MessageBox.Show("Was unable to automatically find the " +
                    //    "`oo2core_6_win64.dll` file in the Sekiro folder. Please copy that file to the " +
                    //    "'lib' folder next to DS Anim Studio.exe in order to load Sekiro files.", "Unable to find compression DLL",
                    //    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

                    //    return false;
                    //}

                    string oodleTarget = Utils.Frankenpath(Main.Directory, "oo2core_6_win64.dll");

                    if (System.IO.File.Exists(oodleSource) && !System.IO.File.Exists(oodleTarget))
                    {
                        System.IO.File.Copy(oodleSource, oodleTarget, true);

                        System.Windows.Forms.MessageBox.Show("Oodle compression library was automatically copied from game directory " +
                            "to editor's '/lib' directory and Sekiro files will load.\n\n", "Required Library Copied", 
                            System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
                    }

                    FileContainer.LoadFromPath(FileContainerName);
                }
                catch (System.DllNotFoundException)
                {
                    //System.Windows.Forms.MessageBox.Show("Cannot open Sekiro files unless you " +
                    //    "copy the `oo2core_6_win64.dll` file from the Sekiro folder into the " +
                    //    "'lib' folder next to DS Anim Studio.exe.", "Additional DLL Required", 
                    //    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

                    System.Windows.Forms.MessageBox.Show("Was unable to automatically find the " +
                        "`oo2core_6_win64.dll` file in the \"Sekiro\" folder. Please copy that file to the " +
                        "'lib' folder next to DS Anim Studio.exe in order to load SDT files.", "Unable to find compression DLL",
                        System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);

                    return false;
                }
                

                if (!FileContainer.AllTAE.Any())
                {
                    return true;
                }

                LoadTaeFileContainer(FileContainer);

                //if (templateName != null)
                //{
                //    LoadTAETemplate(templateName);
                //}

                IsCurrentlyLoadingGraph = false;

                return true;
            }
            else
            {
                return null;
            }
        }

        private void CheckAutoLoadXMLTemplate()
        {
            var objCheck = Utils.GetFileNameWithoutAnyExtensions(Utils.GetFileNameWithoutDirectoryOrExtension(FileContainerName)).ToLower().StartsWith("o") &&
                (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS1 || GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS1R);
            var remoCheck = Utils.GetFileNameWithoutAnyExtensions(Utils.GetFileNameWithoutDirectoryOrExtension(FileContainerName)).ToLower().StartsWith("scn") &&
                (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS1 || GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS1R);

            //var xmlPath = System.IO.Path.Combine(
            //    new System.IO.FileInfo(typeof(TaeEditorScreen).Assembly.Location).DirectoryName,
            //    $@"Res\TAE.Template.{(FileContainer.IsBloodborne ? "BB" : SelectedTae.Format.ToString())}{(objCheck ? ".OBJ" : "")}.xml");

            var xmlPath = System.IO.Path.Combine(
                new System.IO.FileInfo(typeof(TaeEditorScreen).Assembly.Location).DirectoryName,
                $@"Res\TAE.Template.{SelectedTae.Format}{(remoCheck ? ".REMO" : objCheck ? ".OBJ" : "")}.xml");

            if (System.IO.File.Exists(xmlPath))
                LoadTAETemplate(xmlPath);
        }

        public void SaveCurrentFile(Action afterSaveAction = null, string saveMessage = "Saving ANIBND...")
        {
            if (!IsFileOpen)
                return;

            if (IsReadOnlyFileMode)
            {
                System.Windows.Forms.MessageBox.Show("Read-only mode is" +
                    " active so nothing was saved. To open a file in re-saveable mode," +
                    " make sure the Read-Only checkbox is unchecked in the open" +
                    " file dialog.", "Read-Only Mode Active",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Stop);
                return;
            }

            if (System.IO.File.Exists(FileContainerName) && 
                !System.IO.File.Exists(FileContainerName + BackupExtension))
            {
                System.IO.File.Copy(FileContainerName, FileContainerName + BackupExtension);
                System.Windows.Forms.MessageBox.Show(
                    "A backup was not found and was created:\n" + FileContainerName + BackupExtension,
                    "Backup Created", System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Information);
            }

            LoadingTaskMan.DoLoadingTask("SaveFile", saveMessage, progress =>
            {
                FileContainer.SaveToPath(FileContainerName, progress);

                if (Config.LiveRefreshOnSave)
                {
                    LiveRefresh();
                }

                progress.Report(1.0);

                afterSaveAction?.Invoke();
            });

            
        }

        private void LoadAnimIntoGraph(TAE.Animation anim)
        {
            //if (!GraphLookup.ContainsKey(anim))
            //{
            //    var graph = new TaeEditAnimEventGraph(this, false, anim);
            //    GraphLookup.Add(anim, graph);

            //}

            //Graph = GraphLookup[anim];

            if (Graph == null)
                Graph = new TaeEditAnimEventGraph(this, false, anim);
            else
                Graph.ChangeToNewAnimRef(anim);
        }

        private void LoadTaeFileContainer(TaeFileContainer fileContainer)
        {
            TaeExtensionMethods.ClearMemes();
            FileContainer = fileContainer;
            SelectedTae = FileContainer.AllTAE.First();
            GameWindowAsForm.Invoke(new Action(() =>
            {
                // Since form close event is hooked this should
                // take care of nulling it out for us.
                FindValueDialog?.Close();
                ComboMenu?.Close();
                ComboMenu = null;
            }));
            SelectedTaeAnim = SelectedTae.Animations[0];
            AnimationListScreen = new TaeEditAnimList(this);

            
            Graph = null;
            LoadAnimIntoGraph(SelectedTaeAnim);
            IsCurrentlyLoadingGraph = false;

            //if (FileContainer.ContainerType != TaeFileContainer.TaeFileContainerType.TAE)
            //{
            //    TaeInterop.OnLoadANIBND(MenuBar, progress);
            //}
            CheckAutoLoadXMLTemplate();
            SelectNewAnimRef(SelectedTae, SelectedTae.Animations[0]);
            LastFindInfo = null;
        }

        public void RecreateAnimList()
        {
            Vector2 oldScroll = AnimationListScreen.ScrollViewer.Scroll;
            var sectionsCollapsed = AnimationListScreen
                .AnimTaeSections
                .ToDictionary(x => x.Key, x => x.Value.Collapsed);

            AnimationListScreen = new TaeEditAnimList(this);

            foreach (var section in AnimationListScreen.AnimTaeSections)
            {
                if (sectionsCollapsed.ContainsKey(section.Key))
                    section.Value.Collapsed = sectionsCollapsed[section.Key];
            }
            
            AnimationListScreen.ScrollViewer.Scroll = oldScroll;

            HasntSelectedAnAnimYetAfterBuildingAnimList = true;
        }

        public void DuplicateCurrentAnimation()
        {
            TAE.Animation.AnimMiniHeader header = null;



            var newAnimRef = new TAE.Animation(
                SelectedTaeAnim.ID, new TAE.Animation.AnimMiniHeader.Standard(), 
                SelectedTaeAnim.AnimFileName);

            if (SelectedTaeAnim.MiniHeader is TAE.Animation.AnimMiniHeader.Standard stand)
            {
                var standardHeader = new TAE.Animation.AnimMiniHeader.Standard();

                standardHeader.AllowDelayLoad = false;

                if (stand.ImportHKXSourceAnimID >= 0)
                {
                    if (stand.ImportsHKX)
                    {
                        standardHeader.ImportsHKX = true;
                        
                    }

                    if (stand.AllowDelayLoad)
                    {

                    }
                }


                header = standardHeader;
            }
            else if (SelectedTaeAnim.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim imp)
            {
                header = imp;
            }



                var index = SelectedTae.Animations.IndexOf(SelectedTaeAnim);
            SelectedTae.Animations.Insert(index + 1, newAnimRef);

            RecreateAnimList();

            SelectNewAnimRef(SelectedTae, newAnimRef);
        }

        public void LoadTAETemplate(string xmlFile)
        {
            try
            {
                foreach (var tae in FileContainer.AllTAE)
                {
                    tae.ApplyTemplate(TAE.Template.ReadXMLFile(xmlFile));
                }

                foreach (var box in Graph.EventBoxes)
                {
                    box.UpdateEventText();
                }

                var wasSelecting = SelectedEventBox;
                SelectedEventBox = null;
                SelectedEventBox = wasSelecting;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to apply TAE template:\n\n{ex}",
                    "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        public TaeEditorScreen(System.Windows.Forms.Form gameWindowAsForm)
        {
            LoadConfig();   

            gameWindowAsForm.FormClosing += GameWindowAsForm_FormClosing;

            GameWindowAsForm = gameWindowAsForm;

            GameWindowAsForm.MinimumSize = new System.Drawing.Size(1280  - 64, 720 - 64);

            Transport = new TaeTransport(this);

            UpdateLayout();
        }

        public void SetAllTAESectionsCollapsed(bool collapsed)
        {
            foreach (var kvp in AnimationListScreen.AnimTaeSections.Values)
            {
                kvp.Collapsed = collapsed;
            }
        }

        public void LoadContent(ContentManager c)
        {
            Transport.LoadContent(c);
        }

        public void LiveRefresh()
        {
            var chrNameBase = Utils.GetFileNameWithoutAnyExtensions(Utils.GetFileNameWithoutDirectoryOrExtension(FileContainerName)).ToLower();

            if (chrNameBase.StartsWith("c"))
            {
                DSAnimStudio.LiveRefresh.RequestFileReload.RequestReload(
                    DSAnimStudio.LiveRefresh.RequestFileReload.ReloadType.Chr, chrNameBase);
            }
            else if (chrNameBase.StartsWith("o"))
            {
                DSAnimStudio.LiveRefresh.RequestFileReload.RequestReload(
                    DSAnimStudio.LiveRefresh.RequestFileReload.ReloadType.Object, chrNameBase);
            }

            
            //if (FileContainer.ReloadType == TaeFileContainer.TaeFileContainerReloadType.CHR_PTDE || FileContainer.ReloadType == TaeFileContainer.TaeFileContainerReloadType.CHR_DS1R)
            //{
            //    var chrName = Utils.GetFileNameWithoutDirectoryOrExtension(FileContainerName);

            //    //In case of .anibnd.dcx
            //    chrName = Utils.GetFileNameWithoutDirectoryOrExtension(chrName);

            //    if (chrName.ToLower().StartsWith("c") && chrName.Length == 5)
            //    {
            //        if (FileContainer.ReloadType == TaeFileContainer.TaeFileContainerReloadType.CHR_PTDE)
            //        {
            //            TaeLiveRefresh.ForceReloadCHR_PTDE(chrName);
            //        }
            //        else
            //        {
            //            TaeLiveRefresh.ForceReloadCHR_DS1R(chrName);
            //        }
            //    }
            //}
        }

        public void ShowDialogEditTaeHeader()
        {
            PauseUpdate = true;
            var editForm = new TaeEditTaeHeaderForm(SelectedTae);
            editForm.Owner = GameWindowAsForm;
            editForm.ShowDialog();

            if (editForm.WereThingsChanged)
            {
                SelectedTae.SetIsModified(true);

                UpdateSelectedTaeAnimInfoText();
            }

            PauseUpdate = false;
        }

        public bool DoesAnimIDExist(int id)
        {
            foreach (var s in AnimationListScreen.AnimTaeSections.Values)
            {
                var matchedAnims = s.InfoMap.Where(x => x.Value.FullID == id);
                if (matchedAnims.Any())
                {
                    return true;
                }
            }
            return false;
        }

        public bool GotoAnimSectionID(int id, bool scrollOnCenter)
        {
            if (FileContainer.AllTAEDict.Count > 1)
            {
                if (AnimationListScreen.AnimTaeSections.ContainsKey(id))
                {
                    var firstAnimInSection = AnimationListScreen.AnimTaeSections[id].Tae.Animations.FirstOrDefault();
                    if (firstAnimInSection != null)
                    {
                        SelectNewAnimRef(AnimationListScreen.AnimTaeSections[id].Tae, firstAnimInSection, scrollOnCenter);
                        return true;
                    }
                }
            }
            else
            {
                foreach (var anim in SelectedTae.Animations)
                {
                    long sectionOfAnim = GameDataManager.GameTypeHasLongAnimIDs ? (anim.ID / 1_000000) : (anim.ID / 1_0000);
                    if (sectionOfAnim == id)
                    {
                        SelectNewAnimRef(SelectedTae, anim, scrollOnCenter);
                        return true;
                    }
                }
            }

            return false;
        }

        public bool GotoAnimID(long id, bool scrollOnCenter)
        {
            foreach (var s in AnimationListScreen.AnimTaeSections.Values)
            {
                var matchedAnims = s.InfoMap.Where(x => x.Value.FullID == id);
                if (matchedAnims.Any())
                {
                    var anim = matchedAnims.First().Value.Ref;
                    SelectNewAnimRef(s.Tae, anim, scrollOnCenter);
                    return true;
                }
            }
            return false;
        }

        public void ShowDialogEditCurrentAnimInfo()
        {
            DialogManager.ShowTaeAnimPropertiesEditor(SelectedTaeAnim);

            //Task.Run(new Action(() =>
            //{
            //    PauseUpdate = true;
            //    var editForm = new TaeEditAnimPropertiesForm(SelectedTaeAnim, FileContainer.AllTAE.Count() == 1);
            //    editForm.Owner = GameWindowAsForm;
            //    editForm.ShowDialog();

            //    if (editForm.WasAnimDeleted)
            //    {
            //        if (SelectedTae.Animations.Count <= 1)
            //        {
            //            System.Windows.Forms.MessageBox.Show(
            //                "Cannot delete the only animation remaining in the TAE.",
            //                "Can't Delete Last Animation",
            //                System.Windows.Forms.MessageBoxButtons.OK,
            //                System.Windows.Forms.MessageBoxIcon.Stop);
            //        }
            //        else
            //        {
            //            var indexOfCurrentAnim = SelectedTae.Animations.IndexOf(SelectedTaeAnim);
            //            SelectedTae.Animations.Remove(SelectedTaeAnim);
            //            RecreateAnimList();

            //            if (indexOfCurrentAnim > SelectedTae.Animations.Count - 1)
            //                indexOfCurrentAnim = SelectedTae.Animations.Count - 1;

            //            if (indexOfCurrentAnim >= 0)
            //                SelectNewAnimRef(SelectedTae, SelectedTae.Animations[indexOfCurrentAnim]);
            //            else
            //                SelectNewAnimRef(SelectedTae, SelectedTae.Animations[0]);

            //            SelectedTae.SetIsModified(!IsReadOnlyFileMode);
            //        }
            //    }
            //    else
            //    {
            //        bool needsAnimReload = false;
            //        if (editForm.WasAnimIDChanged)
            //        {
            //            SelectedTaeAnim.SetIsModified(!IsReadOnlyFileMode);
            //            SelectedTae.SetIsModified(!IsReadOnlyFileMode);
            //            RecreateAnimList();
            //            UpdateSelectedTaeAnimInfoText();
            //            Graph.InitGhostEventBoxes();
            //            needsAnimReload = true;
            //        }

            //        if (editForm.WereThingsChanged)
            //        {
            //            SelectedTaeAnim.SetIsModified(!IsReadOnlyFileMode);
            //            SelectedTae.SetIsModified(!IsReadOnlyFileMode);
            //            UpdateSelectedTaeAnimInfoText();
            //            Graph.InitGhostEventBoxes();
            //            needsAnimReload = true;
            //        }

            //        if (needsAnimReload)
            //            Graph.ViewportInteractor.OnNewAnimSelected();

            //    }

            //    PauseUpdate = false;
            //}));

            
        }

        private void GameWindowAsForm_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            SaveConfig();

            var unsavedChanges = IsModified && !IsReadOnlyFileMode;

            if (!unsavedChanges && FileContainer != null)
            {
                if (FileContainer.IsModified)
                {
                    unsavedChanges = true;
                }
                else
                {
                    foreach (var tae in FileContainer.AllTAE)
                    {
                        foreach (var anim in tae.Animations)
                        {
                            if (anim.GetIsModified() && !IsReadOnlyFileMode)
                            {
                                unsavedChanges = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (unsavedChanges)
            {
                e.Cancel = true;

                var confirmDlg = System.Windows.Forms.MessageBox.Show(
                    $"File \"{System.IO.Path.GetFileName(FileContainerName)}\" has " +
                    $"unsaved changes. Would you like to save these changes before " +
                    $"closing?", "Save Unsaved Changes?",
                    System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                    System.Windows.Forms.MessageBoxIcon.None);

                if (confirmDlg == System.Windows.Forms.DialogResult.Yes)
                {
                    SaveCurrentFile(afterSaveAction: () =>
                    {
                        Main.REQUEST_EXIT = true;
                    },
                    saveMessage: "Saving ANIBND and then exiting...");
                }
                else if (confirmDlg == System.Windows.Forms.DialogResult.No)
                {
                    e.Cancel = false;
                }
            }
            else
            {
                e.Cancel = false;
            }

            if (!e.Cancel)
            {
                FmodManager.Shutdown();
            }
        }

        private void WinFormsMenuStrip_MenuDeactivate(object sender, EventArgs e)
        {
            //PauseUpdate = false;
        }

        private void WinFormsMenuStrip_MenuActivate(object sender, EventArgs e)
        {
            PauseUpdate = true;
            Input.CursorType = MouseCursorType.Arrow;
        }

        public void DirectOpenFile(string fileName)
        {
            
            void DoActualFileOpen()
            {
                LoadingTaskMan.DoLoadingTask("DirectOpenFile", "Opening ANIBND and associated model(s)...", progress =>
                {
                    string oldFileContainerName = FileContainerName.ToString();
                    var oldFileContainer = FileContainer;

                    FileContainerName = fileName;
                    var loadFileResult = LoadCurrentFile();
                    if (loadFileResult == false)
                    {
                        FileContainerName = oldFileContainerName;
                        FileContainer = oldFileContainer;
                        return;
                    }
                    else if (loadFileResult == null)
                    {
                        if (Config.RecentFilesList.Contains(fileName))
                        {
                            var ask = System.Windows.Forms.MessageBox.Show(
                                $"File '{fileName}' no longer exists. Would you like to " +
                                $"remove it from the recent files list?",
                                "File Does Not Exist",
                                System.Windows.Forms.MessageBoxButtons.YesNo,
                                System.Windows.Forms.MessageBoxIcon.Warning)
                                    == System.Windows.Forms.DialogResult.Yes;

                            if (ask)
                            {
                                if (Config.RecentFilesList.Contains(fileName))
                                    Config.RecentFilesList.Remove(fileName);
                            }
                        }

                        FileContainerName = oldFileContainerName;
                        FileContainer = oldFileContainer;
                        return;
                    }

                    if (!FileContainer.AllTAE.Any())
                    {
                        FileContainerName = oldFileContainerName;
                        FileContainer = oldFileContainer;
                        System.Windows.Forms.MessageBox.Show(
                            "Selected file had no TAE files within. " +
                            "Cancelling load operation.", "Invalid File",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Stop);
                    }
                    else if (loadFileResult == null)
                    {
                        FileContainerName = oldFileContainerName;
                        FileContainer = oldFileContainer;
                        System.Windows.Forms.MessageBox.Show(
                            "File did not exist.", "File Does Not Exist",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Stop);
                    }
                }, disableProgressBarByDefault: true);
            }

            if (FileContainer != null && !IsReadOnlyFileMode && (IsModified || FileContainer.AllTAE.Any(x => x.Animations.Any(a => a.GetIsModified()))))
            {
                var yesNoCancel = System.Windows.Forms.MessageBox.Show(
                    $"File \"{System.IO.Path.GetFileName(FileContainerName)}\" has " +
                    $"unsaved changes. Would you like to save these changes before " +
                    $"loading a new file?", "Save Unsaved Changes?",
                    System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                    System.Windows.Forms.MessageBoxIcon.None);

                if (yesNoCancel == System.Windows.Forms.DialogResult.Yes)
                {
                    SaveCurrentFile(afterSaveAction: () =>
                    {
                        try
                        {
                            DoActualFileOpen();
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Forms.MessageBox.Show(ex.ToString(),
                                "Error While Loading File",
                                System.Windows.Forms.MessageBoxButtons.OK,
                                System.Windows.Forms.MessageBoxIcon.Error);
                        }
                    },
                    saveMessage: "Saving ANIBND then loading new one...");
                    return;
                }
                else if (yesNoCancel == System.Windows.Forms.DialogResult.Cancel)
                {
                    return;
                }
                //If they chose no, continue as normal.
            }

            try
            {
                DoActualFileOpen();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString(), 
                    "Error While Loading File", 
                    System.Windows.Forms.MessageBoxButtons.OK, 
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
                
            
        }

        public void File_Open()
        {
            FmodManager.StopAllSounds();
            if (FileContainer != null && !IsReadOnlyFileMode && FileContainer.AllTAE.Any(x => x.Animations.Any(a => a.GetIsModified())))
            {
                var yesNoCancel = System.Windows.Forms.MessageBox.Show(
                    $"File \"{System.IO.Path.GetFileName(FileContainerName)}\" has " +
                    $"unsaved changes. Would you like to save these changes before " +
                    $"loading a new file?", "Save Unsaved Changes?",
                    System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                    System.Windows.Forms.MessageBoxIcon.None);

                if (yesNoCancel == System.Windows.Forms.DialogResult.Yes)
                {
                    SaveCurrentFile();
                }
                else if (yesNoCancel == System.Windows.Forms.DialogResult.Cancel)
                {
                    return;
                }
                //If they chose no, continue as normal.
            }

            var browseDlg = new System.Windows.Forms.OpenFileDialog()
            {
                Filter = TaeFileContainer.DefaultSaveFilter,
                ValidateNames = true,
                CheckFileExists = true,
                CheckPathExists = true,
                //ShowReadOnly = true,
            };

            if (System.IO.File.Exists(FileContainerName))
            {
                browseDlg.InitialDirectory = System.IO.Path.GetDirectoryName(FileContainerName);
                browseDlg.FileName = System.IO.Path.GetFileName(FileContainerName);
            }

            if (browseDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IsReadOnlyFileMode = browseDlg.ReadOnlyChecked;
                FileContainerName = browseDlg.FileName;

                LoadingTaskMan.DoLoadingTask("File_Open", "Loading ANIBND and associated model(s)...", progress =>
                {
                    var loadFileResult = LoadCurrentFile();
                    if (loadFileResult == false || !FileContainer.AllTAE.Any())
                    {
                        FileContainerName = "";
                        System.Windows.Forms.MessageBox.Show(
                            "Selected file had no TAE files within. " +
                            "Cancelling load operation.", "Invalid File",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Stop);
                    }
                    else if (loadFileResult == null)
                    {
                        FileContainerName = "";
                        System.Windows.Forms.MessageBox.Show(
                            "Selected file did not exist (how did you " +
                            "get this message to appear, anyways?).", "File Does Not Exist",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Stop);
                    }
                }, disableProgressBarByDefault: true);

               
            }
        }

        private string BrowseForXMLTemplateLoop()
        {
            string selectedTemplate = null;
            do
            {
                var templateOption = System.Windows.Forms.MessageBox.Show("Select an XML template file to open.", "Open Template", System.Windows.Forms.MessageBoxButtons.OKCancel, System.Windows.Forms.MessageBoxIcon.Information);
                if (templateOption == System.Windows.Forms.DialogResult.Cancel)
                {
                    return null;
                }
                else
                {
                    selectedTemplate = BrowseForXMLTemplate();
                }
            }
            while (selectedTemplate == null);

            return selectedTemplate;
        }

        private string BrowseForXMLTemplate()
        {
            var browseDlg = new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Open TAE Template XML File",
                Filter = "TAE Templates (*.XML)|*.XML",
                ValidateNames = true,
                CheckFileExists = true,
                CheckPathExists = true,
            };

            browseDlg.InitialDirectory = System.IO.Path.Combine(new System.IO.FileInfo(typeof(TaeEditorScreen).Assembly.Location).DirectoryName, "Res");

            if (browseDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IsReadOnlyFileMode = browseDlg.ReadOnlyChecked;
                return browseDlg.FileName;
            }

            return null;
        }

        public void BrowseForMoreTextures()
        {
            List<string> texturesToLoad = new List<string>();
            lock (Scene._lock_ModelLoad_Draw)
            {
                foreach (var m in Scene.Models)
                {
                    texturesToLoad.AddRange(m.MainMesh.GetAllTexNamesToLoad());
                }
            }


            var browseDlg = new System.Windows.Forms.OpenFileDialog()
            {
                //Filter = "TPF[.DCX]/TFPBHD|*.TPF*",
                Multiselect = true,
                Title = "Load textures from CHRBND(s), TEXBND(s), TPF(s), and/or TPFBHD(s)..."
            };
            if (browseDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var texFileNames = browseDlg.FileNames;
                List<string> bxfDupeCheck = new List<string>();
                LoadingTaskMan.DoLoadingTask("BrowseForEntityTextures", "Scanning files for relevant textures...", progress =>
                {
                    double i = 0;
                    foreach (var tfn in texFileNames)
                    {
                        var shortName = Utils.GetShortIngameFileName(tfn);
                        if (!bxfDupeCheck.Contains(shortName))
                        {
                            if (TPF.Is(tfn))
                            {
                                TexturePool.AddTpfFromPath(tfn);
                            }
                            else
                            {
                                TexturePool.AddSpecificTexturesFromBinder(tfn, texturesToLoad);
                            }

                            bxfDupeCheck.Add(shortName);
                        }
                        progress.Report(++i / texFileNames.Length);
                    }
                    Scene.RequestTextureLoad();
                    progress.Report(1);
                });


            }


        }


        public void File_SaveAs()
        {
            var browseDlg = new System.Windows.Forms.SaveFileDialog()
            {
                Filter = FileContainer?.GetResaveFilter()
                           ?? TaeFileContainer.DefaultSaveFilter,
                ValidateNames = true,
                CheckFileExists = false,
                CheckPathExists = true,
            };

            if (System.IO.File.Exists(FileContainerName))
            {
                browseDlg.InitialDirectory = System.IO.Path.GetDirectoryName(FileContainerName);
                browseDlg.FileName = System.IO.Path.GetFileName(FileContainerName);
            }

            if (browseDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileContainerName = browseDlg.FileName;
                SaveCurrentFile();
            }
        }

        public void ChangeTypeOfSelectedEvent()
        {
            if (SelectedEventBox == null)
                return;

            PauseUpdate = true;

            var changeTypeDlg = new TaeInspectorFormChangeEventType();
            changeTypeDlg.TAEReference = SelectedTae;
            changeTypeDlg.CurrentTemplate = SelectedEventBox.MyEvent.Template;
            changeTypeDlg.NewEventType = SelectedEventBox.MyEvent.Type;

            if (changeTypeDlg.ShowDialog(GameWindowAsForm) == System.Windows.Forms.DialogResult.OK)
            {
                if (changeTypeDlg.NewEventType != SelectedEventBox.MyEvent.Type)
                {
                    var referenceToEventBox = SelectedEventBox;
                    var referenceToPreviousEvent = referenceToEventBox.MyEvent;
                    int index = SelectedTaeAnim.Events.IndexOf(referenceToEventBox.MyEvent);
                    int row = referenceToEventBox.Row;

                    UndoMan.NewAction(
                        doAction: () =>
                        {
                            SelectedTaeAnim.Events.Remove(referenceToPreviousEvent);

                            referenceToEventBox.ChangeEvent(
                                new TAE.Event(referenceToPreviousEvent.StartTime, referenceToPreviousEvent.EndTime,
                                changeTypeDlg.NewEventType, referenceToPreviousEvent.Unk04, SelectedTae.BigEndian,
                                SelectedTae.BankTemplate[changeTypeDlg.NewEventType]), SelectedTaeAnim);

                            SelectedTaeAnim.Events.Insert(index, referenceToEventBox.MyEvent);

                            SelectedEventBox = null;
                            SelectedEventBox = referenceToEventBox;

                            SelectedEventBox.Row = row;

                            Graph.RegisterEventBoxExistance(SelectedEventBox);

                            SelectedTaeAnim.SetIsModified(!IsReadOnlyFileMode);
                            SelectedTae.SetIsModified(!IsReadOnlyFileMode);
                        },
                        undoAction: () =>
                        {
                            SelectedTaeAnim.Events.RemoveAt(index);
                            referenceToEventBox.ChangeEvent(referenceToPreviousEvent, SelectedTaeAnim);
                            SelectedTaeAnim.Events.Insert(index, referenceToPreviousEvent);

                            SelectedEventBox = null;
                            SelectedEventBox = referenceToEventBox;

                            SelectedEventBox.Row = row;

                            Graph.RegisterEventBoxExistance(SelectedEventBox);

                            SelectedTaeAnim.SetIsModified(!IsReadOnlyFileMode);
                            SelectedTae.SetIsModified(!IsReadOnlyFileMode);
                        });
                }
            }

            PauseUpdate = false;
        }

        private (long Upper, long Lower) GetSplitAnimID(long id)
        {
            return ((GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.BB ||
                GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS3) 
                ? (id / 1000000) : (id / 10000),
                (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.BB || 
                GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS3) 
                ? (id % 1000000) : (id % 10000));
        }

        private string HKXNameFromCompositeID(long compositeID)
        {
            if (compositeID < 0)
                return "<NONE>";

            var splitID = GetSplitAnimID(compositeID);

            if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.BB ||
                GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS3)
            {
                return $"a{splitID.Upper:D3}_{splitID.Lower:D6}";
            }
            else
            {
                return $"a{splitID.Upper:D2}_{splitID.Lower:D4}";
            }
        }

        private string HKXSubIDDispNameFromInt(long subID)
        {
            if (FileContainer.AllTAE.Count() == 1)
            {
                return HKXNameFromCompositeID(subID);
            }
            else
            {
                if (GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.BB ||
                GameDataManager.GameType == SoulsAssetPipeline.SoulsGames.DS3)
                {
                    return $"aXXX_{subID:D6}";
                }
                else
                {
                    return $"aXX_{subID:D4}";
                }
            }
            
        }

        public void UpdateSelectedTaeAnimInfoText()
        {
            var stringBuilder = new StringBuilder();

            if (SelectedTaeAnim == null)
            {
                stringBuilder.Append("(No Animation Selected)");
            }
            else
            {
                stringBuilder.Append($"{HKXSubIDDispNameFromInt(SelectedTaeAnim.ID)}");

                if (SelectedTaeAnim.MiniHeader is TAE.Animation.AnimMiniHeader.Standard asStandard)
                {
                    if (asStandard.IsLoopByDefault)
                        stringBuilder.Append($" [{nameof(TAE.Animation.AnimMiniHeader.Standard.IsLoopByDefault)}]");

                    if (asStandard.AllowDelayLoad && asStandard.ImportsHKX)
                    {
                        stringBuilder.Append($" [IMPORTS EVENTS + HKX FROM: {HKXNameFromCompositeID(asStandard.ImportHKXSourceAnimID)}]");
                    }
                    else if (asStandard.AllowDelayLoad && !asStandard.ImportsHKX)
                    {
                        stringBuilder.Append($" [IMPORTS EVENTS FROM: {HKXNameFromCompositeID(asStandard.ImportHKXSourceAnimID)}]");
                    }
                    else if (!asStandard.AllowDelayLoad && asStandard.ImportsHKX)
                    {
                        stringBuilder.Append($" [IMPORTS HKX FROM: {HKXNameFromCompositeID(asStandard.ImportHKXSourceAnimID)}]");
                    }

                }
                else if (SelectedTaeAnim.MiniHeader is TAE.Animation.AnimMiniHeader.ImportOtherAnim asImportOtherAnim)
                {
                    stringBuilder.Append($" [IMPORTS ALL FROM: {HKXNameFromCompositeID(asImportOtherAnim.ImportFromAnimID)}]");
                    stringBuilder.Append($" [UNK: {asImportOtherAnim.Unknown}]");
                }

                //SFTODO

                //if (SelectedTaeAnim.AnimFileReference)
                //{
                //    stringBuilder.Append($" [RefID: {SelectedTaeAnim.RefAnimID}]");
                //}
                //else
                //{
                //    stringBuilder.Append($" [\"{SelectedTaeAnim.Unknown1}\"]");
                //    stringBuilder.Append($" [\"{SelectedTaeAnim.Unknown2}\"]");

                //    //if (SelectedTaeAnim.IsLoopingObjAnim)
                //    //    stringBuilder.Append($" [ObjLoop]");

                //    //if (SelectedTaeAnim.UseHKXOnly)
                //    //    stringBuilder.Append($" [HKXOnly]");

                //    //if (SelectedTaeAnim.TAEDataOnly)
                //    //    stringBuilder.Append($" [TAEOnly]");

                //    //if (SelectedTaeAnim.OriginalAnimID >= 0)
                //    //    stringBuilder.Append($" [OrigID: {SelectedTaeAnim.OriginalAnimID}]");
                //}
            }

            SelectedTaeAnimInfoScrollingText.SetText(stringBuilder.ToString());
        }

        public void SelectEvent(TAE.Event ev)
        {
            var box = Graph.EventBoxes.First(x => x.MyEvent == ev);
            SelectedEventBox = box;

            float left = Graph.ScrollViewer.Scroll.X;
            float top = Graph.ScrollViewer.Scroll.Y;
            float right = Graph.ScrollViewer.Scroll.X + Graph.ScrollViewer.Viewport.Width;
            float bottom = Graph.ScrollViewer.Scroll.Y + Graph.ScrollViewer.Viewport.Height;

            Graph.ScrollViewer.Scroll.X = box.Left - (Graph.ScrollViewer.Viewport.Width / 2f);
            Graph.ScrollViewer.Scroll.Y = (box.Row * Graph.RowHeight) - (Graph.ScrollViewer.Viewport.Height / 2f);
            Graph.ScrollViewer.ClampScroll();
        }

        public void SelectNewAnimRef(TAE tae, TAE.Animation animRef, bool scrollOnCenter = false)
        {
            bool isBlend = (PlaybackCursor.IsPlaying || Graph.ViewportInteractor.IsComboRecording) && 
                Graph.ViewportInteractor.IsBlendingActive &&
                Graph.ViewportInteractor.EntityType != TaeViewportInteractor.TaeEntityType.REMO;

            if (Graph.ViewportInteractor.EntityType == TaeViewportInteractor.TaeEntityType.REMO)
            {
                PlaybackCursor.CurrentTime = 0;
            }

            AnimSwitchRenderCooldown = AnimSwitchRenderCooldownMax;

            PlaybackCursor.IsStepping = false;

            SelectedTae = tae;

            SelectedTaeAnim = animRef;

            UpdateSelectedTaeAnimInfoText();

            if (SelectedTaeAnim != null)
            {
                SelectedEventBox = null;

                //bool wasFirstAnimSelected = false;

                //if (Graph == null)
                //{
                //    wasFirstAnimSelected = true;
                //    Graph = new TaeEditAnimEventGraph(this, false, SelectedTaeAnim);
                //}



                LoadAnimIntoGraph(SelectedTaeAnim);

                if (HasntSelectedAnAnimYetAfterBuildingAnimList)
                {
                    UpdateLayout(); // Fixes scroll when you first open anibnd and when you rebuild anim list.
                    HasntSelectedAnAnimYetAfterBuildingAnimList = false;
                }

                AnimationListScreen.ScrollToAnimRef(SelectedTaeAnim, scrollOnCenter);

                Graph.ViewportInteractor.OnNewAnimSelected();
                Graph.PlaybackCursor.ResetAll();
                Graph.PlaybackCursor.RestartFromBeginning();

                if (!isBlend)
                {
                    Graph.ViewportInteractor.CurrentModel.AnimContainer?.ResetAll();
                    Graph.ViewportInteractor.RootMotionSendHome();
                    Graph.ViewportInteractor.CurrentModel.AnimContainer.Skeleton.CurrentDirection = 0;
                    Graph.ViewportInteractor.CurrentModel.ChrAsm?.RightWeaponModel0?.AnimContainer?.Skeleton?.ResetCurrentDirection();
                    Graph.ViewportInteractor.CurrentModel.ChrAsm?.RightWeaponModel1?.AnimContainer?.Skeleton?.ResetCurrentDirection();
                    Graph.ViewportInteractor.CurrentModel.ChrAsm?.RightWeaponModel2?.AnimContainer?.Skeleton?.ResetCurrentDirection();
                    Graph.ViewportInteractor.CurrentModel.ChrAsm?.RightWeaponModel3?.AnimContainer?.Skeleton?.ResetCurrentDirection();
                    Graph.ViewportInteractor.CurrentModel.ChrAsm?.LeftWeaponModel0?.AnimContainer?.Skeleton?.ResetCurrentDirection();
                    Graph.ViewportInteractor.CurrentModel.ChrAsm?.LeftWeaponModel1?.AnimContainer?.Skeleton?.ResetCurrentDirection();
                    Graph.ViewportInteractor.CurrentModel.ChrAsm?.LeftWeaponModel2?.AnimContainer?.Skeleton?.ResetCurrentDirection();
                    Graph.ViewportInteractor.CurrentModel.ChrAsm?.LeftWeaponModel3?.AnimContainer?.Skeleton?.ResetCurrentDirection();
                    Graph.ViewportInteractor.ResetRootMotion();
                    Graph.ViewportInteractor.RemoveTransition();
                    Graph.ViewportInteractor.CurrentModel.AnimContainer?.ResetAll();
                }
            }
            else
            {
                SelectedEventBox = null;

                Graph = null;
            }
        }

        public void ShowDialogChangeAnimName()
        {
            if (SelectedTaeAnim != null)
            {
                PauseUpdate = true;
                var keyboardTask = KeyboardInput.Show("Set Animation Name", "Set the name of the current animation.", SelectedTaeAnim.AnimFileName);
                keyboardTask.Wait();
                if (keyboardTask.Result != null)
                {
                    if (SelectedTaeAnim.AnimFileName != keyboardTask.Result)
                    {
                        SelectedTaeAnim.AnimFileName = keyboardTask.Result;
                        SelectedTaeAnim.SetIsModified(true);
                    }
                }
                PauseUpdate = false;
            }
        }

        public void ShowDialogFind()
        {
            if (FileContainerName == null || SelectedTae == null)
                return;
            //PauseUpdate = true;

            if (FindValueDialog == null)
            {
                FindValueDialog = new TaeFindValueDialog();
                FindValueDialog.LastFindInfo = LastFindInfo;
                FindValueDialog.EditorRef = this;
                FindValueDialog.Owner = GameWindowAsForm;
                FindValueDialog.Show();
                Main.CenterForm(FindValueDialog);
                FindValueDialog.FormClosed += FindValueDialog_FormClosed;
            }

            

            //var find = KeyboardInput.Show("Quick Find Event", "Finds the very first animation containing the event with the specified ID number or name (according to template).", "");
            //if (int.TryParse(find.Result, out int typeID))
            //{
            //    var gotoAnim = SelectedTae.Animations.Where(x => x.Events.Any(ev => (int)ev.Type == typeID));
            //    if (gotoAnim.Any())
            //        SelectNewAnimRef(SelectedTae, gotoAnim.First());
            //    else
            //        MessageBox.Show("None Found", "No events of that type found within the currently loaded files.", new[] { "OK" });
            //}
            //else 
            //{
            //    var gotoAnim = SelectedTae.Animations.Where(x => x.Events.Any(ev => ev.TypeName == find.Result));
            //    if (gotoAnim.Any())
            //        SelectNewAnimRef(SelectedTae, gotoAnim.First());
            //    else
            //        MessageBox.Show("None Found", "No events of that type found within the currently loaded files.", new[] { "OK" });
            //}


            
            //PauseUpdate = false;
        }

        private void FindValueDialog_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
        {
            FindValueDialog.FormClosed -= FindValueDialog_FormClosed;
            FindValueDialog = null;
        }

        public void ShowDialogGotoAnimSectionID()
        {
            if (FileContainer == null || SelectedTae == null)
                return;

            DialogManager.AskForInputString("Go To Animation Section ID", $"Enter the animation section number (The X part of {GameDataManager.CurrentAnimIDFormatType})\n" +
                "to jump to the first animation in that section.",
                $"", result =>
                {
                    Main.WinForm.Invoke(new Action(() =>
                    {
                        if (int.TryParse(result.Replace("a", "").Replace("_", ""), out int id))
                        {
                            if (!GotoAnimSectionID(id, scrollOnCenter: true))
                            {
                                DialogManager.DialogOK("Goto Failed", $"Unable to find anim section {id}.");
                            }
                        }
                        else
                        {
                            DialogManager.DialogOK("Goto Failed", $"\"{result}\" is not a valid integer.");
                        }
                    }));
                    
                    
                }, canBeCancelled: true);
        }

        public void ShowDialogGotoAnimID()
        {
            if (FileContainer == null || SelectedTae == null)
                return;

            DialogManager.AskForInputString("Go To Animation ID", "Enter the animation ID to jump to.\n" +
                "Accepts the full string with prefix or just the ID as a number.",
                GameDataManager.CurrentAnimIDFormatType.ToString(), result =>
                {
                    Main.WinForm.Invoke(new Action(() =>
                    {
                        if (int.TryParse(result.Replace("a", "").Replace("_", ""), out int id))
                        {
                            if (!GotoAnimID(id, scrollOnCenter: true))
                            {
                                DialogManager.DialogOK("Goto Failed", $"Unable to find anim {id}.");
                            }
                        }
                        else
                        {
                            DialogManager.DialogOK("Goto Failed", $"\"{result}\" is not a valid animation ID.");
                        }
                    }));
                }, canBeCancelled: true);
        }

        public void NextAnim(bool shiftPressed, bool ctrlPressed)
        {
            try
            {
                if (SelectedTae != null)
                {
                    if (SelectedTaeAnim != null)
                    {
                        var taeList = FileContainer.AllTAE.ToList();

                        int currentAnimIndex = SelectedTae.Animations.IndexOf(SelectedTaeAnim);
                        int currentTaeIndex = taeList.IndexOf(SelectedTae);

                        int startingTaeIndex = currentTaeIndex;

                        void DoSmallStep()
                        {
                            if (currentAnimIndex >= taeList[currentTaeIndex].Animations.Count - 1)
                            {
                                currentAnimIndex = 0;

                                if (taeList.Count > 1)
                                {
                                    if (currentTaeIndex >= taeList.Count - 1)
                                    {
                                        currentTaeIndex = 0;
                                    }
                                    else
                                    {
                                        currentTaeIndex++;
                                        if (taeList[currentTaeIndex].Animations.Count == 0)
                                            DoSmallStep();
                                    }
                                }
                            }
                            else
                            {
                                currentAnimIndex++;
                            }
                        }

                        void DoBigStep()
                        {
                            if (taeList.Count > 1)
                            {
                                while (currentTaeIndex == startingTaeIndex)
                                {
                                    DoSmallStep();
                                }

                                currentAnimIndex = 0;
                            }
                            else
                            {
                                var startSection = GameDataManager.GameTypeHasLongAnimIDs ? (SelectedTaeAnim.ID / 1_000000) : (SelectedTaeAnim.ID / 1_0000);

                                //long stopAtSection = -1;
                                for (int i = currentAnimIndex; i < SelectedTae.Animations.Count; i++)
                                {
                                    var thisSection = GameDataManager.GameTypeHasLongAnimIDs ? (SelectedTae.Animations[i].ID / 1_000000) : (SelectedTae.Animations[i].ID / 1_0000);
                                    if (startSection != thisSection)
                                    {
                                        currentAnimIndex = i;
                                        return;
                                        //if (stopAtSection == -1)
                                        //{
                                        //    stopAtSection = thisSection;
                                        //    currentAnimIndex = i;
                                        //}
                                        //else
                                        //{
                                        //    if (thisSection == stopAtSection)
                                        //    {
                                        //        currentAnimIndex = i;
                                        //    }
                                        //    else
                                        //    {
                                        //        return;
                                        //    }
                                        //}
                                    }
                                }

                                currentAnimIndex = 0;
                            }

                            
                        }

                        void DoStep()
                        {
                            if (ctrlPressed)
                                DoBigStep();
                            else
                                DoSmallStep();
                        }

                        if (shiftPressed)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                DoStep();
                            }
                        }
                        else
                        {
                            DoStep();
                        }

                        try
                        {
                            SelectNewAnimRef(taeList[currentTaeIndex], taeList[currentTaeIndex].Animations[currentAnimIndex], scrollOnCenter: Input.ShiftHeld || Input.CtrlHeld);
                        }
                        catch// (Exception innerEx)
                        {
                            //Console.WriteLine("FATCAT");
                        }
                        
                    }
                }
            }
            catch// (Exception ex)
            {
                //Console.WriteLine("Fatcat");
            }
        }

        public void PrevAnim(bool shiftPressed, bool ctrlPressed)
        {
            try
            {
                if (SelectedTae != null)
                {
                    if (SelectedTaeAnim != null)
                    {
                        var taeList = FileContainer.AllTAE.ToList();

                        int currentAnimIndex = SelectedTae.Animations.IndexOf(SelectedTaeAnim);

                        int currentTaeIndex = taeList.IndexOf(SelectedTae);

                        int startingTaeIndex = currentTaeIndex;

                        void DoSmallStep()
                        {
                            if (currentAnimIndex <= 0)
                            {
                                if (taeList.Count > 1)
                                {
                                    if (currentTaeIndex <= 0)
                                    {
                                        currentTaeIndex = taeList.Count - 1;
                                    }
                                    else
                                    {
                                        currentTaeIndex--;
                                        if (taeList[currentTaeIndex].Animations.Count == 0)
                                            DoSmallStep();
                                    }
                                }

                                currentAnimIndex = taeList[currentTaeIndex].Animations.Count - 1;
                            }
                            else
                            {
                                currentAnimIndex--;
                            }
                        }

                        void DoBigStep()
                        {
                            if (taeList.Count > 1)
                            {
                                while (currentTaeIndex == startingTaeIndex)
                                {
                                    DoSmallStep();
                                }

                                currentAnimIndex = 0;
                            }
                            else
                            {
                                var startSection = GameDataManager.GameTypeHasLongAnimIDs ? (SelectedTaeAnim.ID / 1_000000) : (SelectedTaeAnim.ID / 1_0000);
                                if (currentAnimIndex == 0)
                                    currentAnimIndex = SelectedTae.Animations.Count - 1;
                                long stopAtSection = -1;
                                for (int i = currentAnimIndex; i >= 0; i--)
                                {
                                    var thisSection = GameDataManager.GameTypeHasLongAnimIDs ? (SelectedTae.Animations[i].ID / 1_000000) : (SelectedTae.Animations[i].ID / 1_0000);
                                    if (startSection != thisSection)
                                    {
                                        if (stopAtSection == -1)
                                        {
                                            stopAtSection = thisSection;
                                            currentAnimIndex = i;
                                        }
                                        else
                                        {
                                            if (thisSection == stopAtSection)
                                            {
                                                currentAnimIndex = i;
                                            }
                                            else
                                            {
                                                return;
                                            }
                                        }
                                    }
                                }

                                //currentAnimIndex = 0;
                            }

                            
                        }

                        void DoStep()
                        {
                            if (ctrlPressed)
                                DoBigStep();
                            else
                                DoSmallStep();
                        }

                        if (shiftPressed)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                DoStep();
                            }
                        }
                        else
                        {
                            DoStep();
                        }

                        SelectNewAnimRef(taeList[currentTaeIndex], taeList[currentTaeIndex].Animations[currentAnimIndex], scrollOnCenter: Input.ShiftHeld || Input.CtrlHeld);
                    }
                }
            }
            catch
            {

            }
        }

        public void TransportNextFrame()
        {
            PlaybackCursor.IsPlaying = false;
            PlaybackCursor.IsStepping = true;

            PlaybackCursor.CurrentTime += PlaybackCursor.CurrentSnapInterval;
            PlaybackCursor.CurrentTime = Math.Floor(PlaybackCursor.CurrentTime / PlaybackCursor.CurrentSnapInterval) * PlaybackCursor.CurrentSnapInterval;

            if (PlaybackCursor.CurrentTime > PlaybackCursor.MaxTime)
                PlaybackCursor.CurrentTime %= PlaybackCursor.MaxTime;

            //PlaybackCursor.StartTime = PlaybackCursor.CurrentTime;
            Graph.ScrollToPlaybackCursor(1);

        }

        public void TransportPreviousFrame()
        {
            PlaybackCursor.IsPlaying = false;
            PlaybackCursor.IsStepping = true;

            PlaybackCursor.CurrentTime -= PlaybackCursor.CurrentSnapInterval;
            PlaybackCursor.CurrentTime = Math.Floor(PlaybackCursor.CurrentTime / PlaybackCursor.CurrentSnapInterval) * PlaybackCursor.CurrentSnapInterval;

            if (PlaybackCursor.CurrentTime < 0)
                PlaybackCursor.CurrentTime += PlaybackCursor.MaxTime;

            //PlaybackCursor.StartTime = PlaybackCursor.CurrentTime;

            Graph.ScrollToPlaybackCursor(1);
        }

        public void ReselectCurrentAnimation()
        {
            SelectNewAnimRef(SelectedTae, SelectedTaeAnim);
        }

        public void HardReset()
        {
            if (Graph == null)
                return;
            Graph.ViewportInteractor.CurrentModel.AnimContainer?.ResetAll();
            Graph.ViewportInteractor.RootMotionSendHome();
            Graph.ViewportInteractor.CurrentModel.AnimContainer.Skeleton.CurrentDirection = 0;
            Graph.ViewportInteractor.CurrentModel.ChrAsm?.RightWeaponModel0?.AnimContainer?.Skeleton?.ResetCurrentDirection();
            Graph.ViewportInteractor.CurrentModel.ChrAsm?.RightWeaponModel1?.AnimContainer?.Skeleton?.ResetCurrentDirection();
            Graph.ViewportInteractor.CurrentModel.ChrAsm?.RightWeaponModel2?.AnimContainer?.Skeleton?.ResetCurrentDirection();
            Graph.ViewportInteractor.CurrentModel.ChrAsm?.RightWeaponModel3?.AnimContainer?.Skeleton?.ResetCurrentDirection();
            Graph.ViewportInteractor.CurrentModel.ChrAsm?.LeftWeaponModel0?.AnimContainer?.Skeleton?.ResetCurrentDirection();
            Graph.ViewportInteractor.CurrentModel.ChrAsm?.LeftWeaponModel1?.AnimContainer?.Skeleton?.ResetCurrentDirection();
            Graph.ViewportInteractor.CurrentModel.ChrAsm?.LeftWeaponModel2?.AnimContainer?.Skeleton?.ResetCurrentDirection();
            Graph.ViewportInteractor.CurrentModel.ChrAsm?.LeftWeaponModel3?.AnimContainer?.Skeleton?.ResetCurrentDirection();
            Graph.ViewportInteractor.ResetRootMotion();
            Graph.ViewportInteractor.RemoveTransition();
            Graph.PlaybackCursor.RestartFromBeginning();
            Graph.ViewportInteractor.CurrentModel.AnimContainer?.ResetAll();
        }

        public void Update()
        {
            if (QueuedChangeEventType)
            {
                if (SingleEventBoxSelected)
                    ChangeTypeOfSelectedEvent();
                QueuedChangeEventType = false;
            }

            if (ImporterWindow_FLVER2 == null || ImporterWindow_FLVER2.IsDisposed || !ImporterWindow_FLVER2.Visible)
            {
                ImporterWindow_FLVER2?.Dispose();
                ImporterWindow_FLVER2 = null;
            }
            ImporterWindow_FLVER2?.UpdateGuiLockout();

            if (IsDelayLoadConfig > 0)
            {
                LoadConfig();
                IsDelayLoadConfig--;
            }

            if (!Input.LeftClickHeld)
            {
                Graph?.ReleaseCurrentDrag();
            }

            if (!Main.Active)
            {
                FmodManager.StopAllSounds();
            }

            //TODO: CHECK THIS
            PauseUpdate = OSD.Hovered;

            if (!PauseUpdate)
            {
                //bad hotfix warning
                if (Graph != null &&
                    !Graph.ScrollViewer.DisableVerticalScroll &&
                    Input.MousePosition.X >=
                        Graph.Rect.Right -
                        Graph.ScrollViewer.ScrollBarThickness &&
                    Input.MousePosition.X < DividerRightGrabStartX &&
                    Input.MousePosition.Y >= Graph.Rect.Top &&
                    Input.MousePosition.Y < Graph.Rect.Bottom)
                {
                    Input.CursorType = MouseCursorType.Arrow;
                }

                // Another bad hotfix
                Rectangle killCursorRect = new Rectangle(
                        (int)(DividerLeftGrabEndX),
                        0,
                        (int)(DividerRightGrabStartX - DividerLeftGrabEndX),
                        TopOfGraphAnimInfoMargin + Rect.Top + TopMenuBarMargin);

                if (killCursorRect.Contains(Input.MousePositionPoint))
                {
                    Input.CursorType = MouseCursorType.Arrow;
                }

                if (!Input.LeftClickHeld)
                    Graph?.MouseReleaseStuff();
            }

            //if (MultiSelectedEventBoxes.Count > 0 && multiSelectedEventBoxesCountLastFrame < MultiSelectedEventBoxes.Count)
            //{
            //    if (Config.UseGamesMenuSounds)
            //        FmodManager.PlaySE("f000000000");
            //}

            multiSelectedEventBoxesCountLastFrame = MultiSelectedEventBoxes.Count;

            // Always update playback regardless of GUI memes.
            // Still only allow hitting spacebar to play/pause
            // if the window is in focus.
            // Also for Interactor
            if (Graph != null)
            {
                Graph.UpdatePlaybackCursor(allowPlayPauseInput: Main.Active);
                Graph.ViewportInteractor?.GeneralUpdate();
            }

            //if (MenuBar.IsAnyMenuOpenChanged)
            //{
            //    ButtonEditCurrentAnimInfo.Visible = !MenuBar.IsAnyMenuOpen;
            //    ButtonEditCurrentTaeHeader.Visible = !MenuBar.IsAnyMenuOpen;
            //    ButtonGotoEventSource.Visible = !MenuBar.IsAnyMenuOpen;
            //    inspectorWinFormsControl.Visible = !MenuBar.IsAnyMenuOpen;
            //}

            if (OSD.Hovered)
            {
                PauseUpdate = true;
            }


            if (PauseUpdate)
            {
                return;
            }

            Transport.Update(Main.DELTA_UPDATE);

            bool isOtherPaneFocused = ModelViewerBounds.Contains((int)Input.LeftClickDownAnchor.X, (int)Input.LeftClickDownAnchor.Y);

            if (Main.Active)
            {
                if (Input.KeyDown(Keys.Escape))
                    FmodManager.StopAllSounds();

                if (Input.KeyDown(Microsoft.Xna.Framework.Input.Keys.F1))
                    ChangeTypeOfSelectedEvent();

                if (Input.KeyDown(Microsoft.Xna.Framework.Input.Keys.F2))
                    ShowDialogChangeAnimName();

                if (Input.KeyDown(Microsoft.Xna.Framework.Input.Keys.F3))
                    ShowDialogEditCurrentAnimInfo();

                if (Input.KeyDown(Microsoft.Xna.Framework.Input.Keys.F4))
                    GoToEventSource();

                if (Input.KeyDown(Microsoft.Xna.Framework.Input.Keys.F5))
                    LiveRefresh();

                if (Input.KeyDown(Microsoft.Xna.Framework.Input.Keys.F8))
                    ShowComboMenu();

                var zHeld = Input.KeyHeld(Microsoft.Xna.Framework.Input.Keys.Z);
                var yHeld = Input.KeyHeld(Microsoft.Xna.Framework.Input.Keys.Y);

                if (Input.CtrlHeld && !Input.ShiftHeld && !Input.AltHeld)
                {
                    if ((Input.KeyDown(Keys.OemPlus) || Input.KeyDown(Keys.Add)) && !isOtherPaneFocused)
                    {
                        Graph?.ZoomInOneNotch(
                            (float)(
                            (Graph.PlaybackCursor.GUICurrentTime * Graph.SecondsPixelSize)
                            - Graph.ScrollViewer.Scroll.X));
                    }
                    else if ((Input.KeyDown(Keys.OemMinus) || Input.KeyDown(Keys.Subtract)) && !isOtherPaneFocused)
                    {
                        Graph?.ZoomOutOneNotch(
                            (float)(
                            (Graph.PlaybackCursor.GUICurrentTime * Graph.SecondsPixelSize)
                            - Graph.ScrollViewer.Scroll.X));
                    }
                    else if ((Input.KeyDown(Keys.D0) || Input.KeyDown(Keys.NumPad0)) && !isOtherPaneFocused)
                    {
                        Graph?.ResetZoom(0);
                    }
                    else if (!CurrentlyEditingSomethingInInspector && Input.KeyDown(Keys.C) &&
                        WhereCurrentMouseClickStarted != ScreenMouseHoverKind.Inspector && !isOtherPaneFocused)
                    {
                        Graph?.DoCopy();
                    }
                    else if (!CurrentlyEditingSomethingInInspector && Input.KeyDown(Keys.X) &&
                        WhereCurrentMouseClickStarted != ScreenMouseHoverKind.Inspector && !isOtherPaneFocused)
                    {
                        Graph?.DoCut();
                    }
                    else if (!CurrentlyEditingSomethingInInspector && Input.KeyDown(Keys.V) &&
                        WhereCurrentMouseClickStarted != ScreenMouseHoverKind.Inspector && !isOtherPaneFocused)
                    {
                        Graph?.DoPaste(isAbsoluteLocation: false);
                    }
                    else if (!CurrentlyEditingSomethingInInspector && Input.KeyDown(Keys.A) && !isOtherPaneFocused)
                    {
                        if (Graph != null && Graph.currentDrag.DragType == BoxDragType.None)
                        {
                            SelectedEventBox = null;
                            MultiSelectedEventBoxes.Clear();
                            foreach (var box in Graph.EventBoxes)
                            {
                                MultiSelectedEventBoxes.Add(box);
                            }
                        }
                    }
                    else if (Input.KeyDown(Keys.F))
                    {
                        ShowDialogFind();
                    }
                    else if (Input.KeyDown(Keys.G))
                    {
                        ShowDialogGotoAnimID();
                    }
                    else if (Input.KeyDown(Keys.H))
                    {
                        ShowDialogGotoAnimSectionID();
                    }
                    else if (Input.KeyDown(Keys.S))
                    {
                        SaveCurrentFile();
                    }
                    else if (Graph != null && Input.KeyDown(Keys.R))
                    {
                        HardReset();
                    }
                }

                if (Input.CtrlHeld && Input.ShiftHeld && !Input.AltHeld)
                {
                    if (Input.KeyDown(Keys.V) && !isOtherPaneFocused)
                    {
                        Graph.DoPaste(isAbsoluteLocation: true);
                    }
                    else if (Input.KeyDown(Keys.S))
                    {
                        File_SaveAs();
                    }
                }

                if (!Input.CtrlHeld && Input.ShiftHeld && !Input.AltHeld)
                {
                    if (Input.KeyDown(Keys.D))
                    {
                        if (SelectedEventBox != null)
                            SelectedEventBox = null;
                        if (MultiSelectedEventBoxes.Count > 0)
                            MultiSelectedEventBoxes.Clear();
                    }
                }

                if (Input.KeyDown(Keys.Delete) && !isOtherPaneFocused)
                {
                    Graph.DeleteSelectedEvent();
                }

                //if (Graph != null && Input.KeyDown(Keys.Home) && !Graph.PlaybackCursor.Scrubbing)
                //{
                //    if (CtrlHeld)
                //        Graph.PlaybackCursor.IsPlaying = false;

                //    Graph.PlaybackCursor.CurrentTime = ShiftHeld ? 0 : Graph.PlaybackCursor.StartTime;
                //    Graph.ViewportInteractor.ResetRootMotion(0);

                //    Graph.ScrollToPlaybackCursor(1);
                //}



                //if (Graph != null && Input.KeyDown(Keys.End) && !Graph.PlaybackCursor.Scrubbing)
                //{
                //    if (CtrlHeld)
                //        Graph.PlaybackCursor.IsPlaying = false;

                //    Graph.PlaybackCursor.CurrentTime = Graph.PlaybackCursor.MaxTime;
                //    Graph.ViewportInteractor.ResetRootMotion((float)Graph.PlaybackCursor.MaxFrame);

                //    Graph.ScrollToPlaybackCursor(1);
                //}

                NextAnimRepeaterButton.Update(GamePadState.Default, Main.DELTA_UPDATE, Input.KeyHeld(Keys.Down) && !Input.KeyHeld(Keys.Up));

                if (NextAnimRepeaterButton.State)
                {
                    Graph?.ViewportInteractor?.CancelCombo();
                    NextAnim(Input.ShiftHeld, Input.CtrlHeld);
                }

                PrevAnimRepeaterButton.Update(GamePadState.Default, Main.DELTA_UPDATE, Input.KeyHeld(Keys.Up) && !Input.KeyHeld(Keys.Down));

                if (PrevAnimRepeaterButton.State)
                {
                    Graph?.ViewportInteractor?.CancelCombo();
                    PrevAnim(Input.ShiftHeld, Input.CtrlHeld);
                }

                if (PlaybackCursor != null)
                {
                    NextFrameRepeaterButton.Update(GamePadState.Default, Main.DELTA_UPDATE, Input.KeyHeld(Keys.Right));

                    if (NextFrameRepeaterButton.State)
                    {
                        TransportNextFrame();
                    }

                    PrevFrameRepeaterButton.Update(GamePadState.Default, Main.DELTA_UPDATE, Input.KeyHeld(Keys.Left));

                    if (PrevFrameRepeaterButton.State)
                    {
                        TransportPreviousFrame();
                    }
                }

                if (Input.KeyDown(Keys.Space) && Input.CtrlHeld && !Input.AltHeld)
                {
                    if (SelectedTae != null)
                    {
                        if (SelectedTaeAnim != null)
                        {
                            SelectNewAnimRef(SelectedTae, SelectedTaeAnim);
                            if (Input.ShiftHeld)
                            {
                                Graph?.ViewportInteractor?.RemoveTransition();
                            }
                        }
                    }
                }

                if (Input.KeyDown(Keys.Back) && !isOtherPaneFocused)
                {
                    Graph?.ViewportInteractor?.RemoveTransition();
                }

                if (UndoButton.Update(Main.DELTA_UPDATE, (Input.CtrlHeld && !Input.ShiftHeld && !Input.AltHeld) && (zHeld && !yHeld)) && !isOtherPaneFocused)
                {
                    UndoMan.Undo();
                }

                if (RedoButton.Update(Main.DELTA_UPDATE, (Input.CtrlHeld && !Input.ShiftHeld && !Input.AltHeld) && (!zHeld && yHeld)) && !isOtherPaneFocused)
                {
                    UndoMan.Redo();
                }
            }

            if (!Input.LeftClickHeld)
                WhereCurrentMouseClickStarted = ScreenMouseHoverKind.None;


            if (WhereCurrentMouseClickStarted == ScreenMouseHoverKind.None)
            {
                if (Input.MousePosition.Y >= TopMenuBarMargin && Input.MousePosition.Y <= Rect.Bottom
                    && Input.MousePosition.X >= DividerLeftGrabStartX && Input.MousePosition.X <= DividerLeftGrabEndX)
                {
                    MouseHoverKind = ScreenMouseHoverKind.DividerBetweenCenterAndLeftPane;
                    //Input.CursorType = MouseCursorType.DragX;
                    if (Input.LeftClickDown)
                    {
                        CurrentDividerDragMode = DividerDragMode.LeftVertical;
                        WhereCurrentMouseClickStarted = ScreenMouseHoverKind.DividerBetweenCenterAndLeftPane;
                    }
                }
                else if (Input.MousePosition.Y >= TopMenuBarMargin && Input.MousePosition.Y <= Rect.Bottom
                    && Input.MousePosition.X >= DividerRightGrabStartX && Input.MousePosition.X <= DividerRightGrabEndX)
                {
                    MouseHoverKind = ScreenMouseHoverKind.DividerBetweenCenterAndRightPane;
                    //Input.CursorType = MouseCursorType.DragX;
                    if (Input.LeftClickDown)
                    {
                        CurrentDividerDragMode = DividerDragMode.RightVertical;
                        WhereCurrentMouseClickStarted = ScreenMouseHoverKind.DividerBetweenCenterAndRightPane;
                    }
                }
                else if (Input.MousePosition.X >= RightSectionStartX && Input.MousePosition.X <= Rect.Right
                    && Input.MousePosition.Y >= DividerRightPaneHorizontalGrabStartY && Input.MousePosition.Y <= DividerRightPaneHorizontalGrabEndY)
                {
                    MouseHoverKind = ScreenMouseHoverKind.DividerRightPaneHorizontal;
                    //Input.CursorType = MouseCursorType.DragX;
                    if (Input.LeftClickDown)
                    {
                        CurrentDividerDragMode = DividerDragMode.RightPaneHorizontal;
                        WhereCurrentMouseClickStarted = ScreenMouseHoverKind.DividerRightPaneHorizontal;
                    }
                }
                else if (MouseHoverKind == ScreenMouseHoverKind.DividerBetweenCenterAndLeftPane
                    || MouseHoverKind == ScreenMouseHoverKind.DividerBetweenCenterAndRightPane
                    || MouseHoverKind == ScreenMouseHoverKind.DividerRightPaneHorizontal)
                {
                    MouseHoverKind = ScreenMouseHoverKind.None;
                }
            }

            if (MouseHoverKind == ScreenMouseHoverKind.DividerBetweenCenterAndLeftPane
                || WhereCurrentMouseClickStarted == ScreenMouseHoverKind.DividerBetweenCenterAndLeftPane
                || MouseHoverKind == ScreenMouseHoverKind.DividerBetweenCenterAndRightPane
                || WhereCurrentMouseClickStarted == ScreenMouseHoverKind.DividerBetweenCenterAndRightPane)
            {
                Input.CursorType = MouseCursorType.DragX;
            }
            else if (MouseHoverKind == ScreenMouseHoverKind.DividerRightPaneHorizontal
                || WhereCurrentMouseClickStarted == ScreenMouseHoverKind.DividerRightPaneHorizontal)
            {
                Input.CursorType = MouseCursorType.DragY;
            }

            if (CurrentDividerDragMode == DividerDragMode.LeftVertical)
            {
                if (Input.LeftClickHeld)
                {
                    //Input.CursorType = MouseCursorType.DragX;
                    LeftSectionWidth = MathHelper.Max((Input.MousePosition.X - Rect.X) - (DividerVisiblePad / 2), LeftSectionWidthMin);
                    LeftSectionWidth = MathHelper.Min(LeftSectionWidth, Rect.Width - MiddleSectionWidthMin - RightSectionWidth - (DividerVisiblePad * 2));
                    MouseHoverKind = ScreenMouseHoverKind.DividerBetweenCenterAndLeftPane;
                    Main.RequestViewportRenderTargetResolutionChange = true;
                    Main.RequestHideOSD = Main.RequestHideOSD_MAX;
                }
                else
                {
                    //Input.CursorType = MouseCursorType.Arrow;
                    CurrentDividerDragMode = DividerDragMode.None;
                    WhereCurrentMouseClickStarted = ScreenMouseHoverKind.None;
                }
            }
            else if (CurrentDividerDragMode == DividerDragMode.RightVertical)
            {
                if (Input.LeftClickHeld)
                {
                    //Input.CursorType = MouseCursorType.DragX;
                    RightSectionWidth = MathHelper.Max((Rect.Right - Input.MousePosition.X) + (DividerVisiblePad / 2), RightSectionWidthMin);
                    RightSectionWidth = MathHelper.Min(RightSectionWidth, Rect.Width - MiddleSectionWidthMin - LeftSectionWidth - (DividerVisiblePad * 2));
                    MouseHoverKind = ScreenMouseHoverKind.DividerBetweenCenterAndRightPane;
                    Main.RequestViewportRenderTargetResolutionChange = true;
                    Main.RequestHideOSD = Main.RequestHideOSD_MAX;
                }
                else
                {
                    //Input.CursorType = MouseCursorType.Arrow;
                    CurrentDividerDragMode = DividerDragMode.None;
                    WhereCurrentMouseClickStarted = ScreenMouseHoverKind.None;
                }
            }
            else if (CurrentDividerDragMode == DividerDragMode.RightPaneHorizontal)
            {
                if (Input.LeftClickHeld)
                {
                    //Input.CursorType = MouseCursorType.DragY;
                    TopRightPaneHeight = MathHelper.Max((Input.MousePosition.Y - Rect.Top - TopMenuBarMargin - TransportHeight) + (DividerVisiblePad / 2), TopRightPaneHeightMinNew);
                    TopRightPaneHeight = MathHelper.Min(TopRightPaneHeight, Rect.Height - BottomRightPaneHeightMinNew - DividerVisiblePad - TopMenuBarMargin - TransportHeight);
                    MouseHoverKind = ScreenMouseHoverKind.DividerBetweenCenterAndRightPane;
                    Main.RequestViewportRenderTargetResolutionChange = true;
                    Main.RequestHideOSD = Main.RequestHideOSD_MAX;
                }
                else
                {
                    //Input.CursorType = MouseCursorType.Arrow;
                    CurrentDividerDragMode = DividerDragMode.None;
                    WhereCurrentMouseClickStarted = ScreenMouseHoverKind.None;
                }
            }

            LeftSectionWidth = MathHelper.Max(LeftSectionWidth, LeftSectionWidthMin);
            LeftSectionWidth = MathHelper.Min(LeftSectionWidth, Rect.Width - MiddleSectionWidthMin - RightSectionWidthMin - (DividerVisiblePad * 2));

            RightSectionWidth = MathHelper.Max(RightSectionWidth, RightSectionWidthMin);
            RightSectionWidth = MathHelper.Min(RightSectionWidth, Rect.Width - MiddleSectionWidthMin - LeftSectionWidthMin - (DividerVisiblePad * 2));

            TopRightPaneHeight = MathHelper.Max(TopRightPaneHeight, TopRightPaneHeightMinNew);
            TopRightPaneHeight = MathHelper.Min(TopRightPaneHeight, Rect.Height - BottomRightPaneHeightMinNew - DividerVisiblePad - TopMenuBarMargin - TransportHeight);

            if (!Rect.Contains(Input.MousePositionPoint))
            {
                MouseHoverKind = ScreenMouseHoverKind.None;
            }

            // Very specific edge case to handle before you load an anibnd so that
            // it won't have the resize cursor randomly. This box spans all the way
            // from left of screen to the hitbox of the right vertical divider and
            // just immediately clears the resize cursor in that entire huge region.
            if (AnimationListScreen == null && Graph == null
                    && new Rectangle(Rect.Left, Rect.Top, (int)(DividerRightGrabStartX - Rect.Left), Rect.Height).Contains(Input.MousePositionPoint))
            {
                MouseHoverKind = ScreenMouseHoverKind.None;
                Input.CursorType = MouseCursorType.Arrow;
            }

            // Check if currently dragging to resize panes.
            if (WhereCurrentMouseClickStarted == ScreenMouseHoverKind.DividerBetweenCenterAndLeftPane
                || WhereCurrentMouseClickStarted == ScreenMouseHoverKind.DividerBetweenCenterAndRightPane)
            {
                Input.CursorType = MouseCursorType.DragX;
                GFX.World.DisableAllInput = true;
                return;
            }
            else if (WhereCurrentMouseClickStarted == ScreenMouseHoverKind.DividerRightPaneHorizontal)
            {
                Input.CursorType = MouseCursorType.DragY;
                GFX.World.DisableAllInput = true;
                return;
            }
            else if (WhereCurrentMouseClickStarted == ScreenMouseHoverKind.ShaderAdjuster)
            {
                GFX.World.DisableAllInput = true;
                return;
            }
            else if (!(MouseHoverKind == ScreenMouseHoverKind.DividerBetweenCenterAndRightPane
                || MouseHoverKind == ScreenMouseHoverKind.DividerBetweenCenterAndLeftPane
                || MouseHoverKind == ScreenMouseHoverKind.DividerRightPaneHorizontal))
            {
                if (AnimationListScreen != null && AnimationListScreen.Rect.Contains(Input.MousePositionPoint))
                    MouseHoverKind = ScreenMouseHoverKind.AnimList;
                else if (Graph != null && Graph.Rect.Contains(Input.MousePositionPoint))
                    MouseHoverKind = ScreenMouseHoverKind.EventGraph;
                else if (
                    new Rectangle(
                        (int)(ImGuiEventInspectorPos.X),
                        (int)(ImGuiEventInspectorPos.Y),
                        (int)(ImGuiEventInspectorSize.X),
                        (int)(ImGuiEventInspectorSize.Y)
                        ).InverseDpiScaled()
                        .Contains(Input.MousePositionPoint))
                    MouseHoverKind = ScreenMouseHoverKind.Inspector;
                //else if (ShaderAdjuster.Bounds.Contains(new System.Drawing.Point(Input.MousePositionPoint.X, Input.MousePositionPoint.Y)))
                //    MouseHoverKind = ScreenMouseHoverKind.ShaderAdjuster;
                else if (
                    ModelViewerBounds_InputArea.Contains(Input.MousePositionPoint))
                {
                    MouseHoverKind = ScreenMouseHoverKind.ModelViewer;
                }
                else
                    MouseHoverKind = ScreenMouseHoverKind.None;

                if (Input.LeftClickDown)
                {
                    WhereCurrentMouseClickStarted = MouseHoverKind;
                }

                if (AnimationListScreen != null)
                {

                    if (MouseHoverKind == ScreenMouseHoverKind.AnimList ||
                        WhereCurrentMouseClickStarted == ScreenMouseHoverKind.AnimList)
                    {
                        Input.CursorType = MouseCursorType.Arrow;
                        AnimationListScreen.Update(Main.DELTA_UPDATE,
                            allowMouseUpdate: CurrentDividerDragMode == DividerDragMode.None);
                    }
                    else
                    {
                        AnimationListScreen.UpdateMouseOutsideRect(Main.DELTA_UPDATE,
                            allowMouseUpdate: CurrentDividerDragMode == DividerDragMode.None);
                    }
                }

                if (Graph != null)
                {
                    Graph.UpdateMiddleClickPan();

                    if (!Graph.Rect.Contains(Input.MousePositionPoint))
                    {
                        HoveringOverEventBox = null;
                    }

                    if (MouseHoverKind == ScreenMouseHoverKind.EventGraph ||
                        WhereCurrentMouseClickStarted == ScreenMouseHoverKind.EventGraph)
                    {
                        Graph.Update(allowMouseUpdate: CurrentDividerDragMode == DividerDragMode.None);
                    }
                    else
                    {
                        Graph.UpdateMouseOutsideRect(Main.DELTA_UPDATE, allowMouseUpdate: CurrentDividerDragMode == DividerDragMode.None);
                    }
                }

                if (MouseHoverKind == ScreenMouseHoverKind.ModelViewer ||
                    WhereCurrentMouseClickStarted == ScreenMouseHoverKind.ModelViewer)
                {
                    Input.CursorType = MouseCursorType.Arrow;
                    GFX.World.DisableAllInput = false;
                }
                else
                {
                    //GFX.World.DisableAllInput = true;
                }

                if (MouseHoverKind == ScreenMouseHoverKind.Inspector ||
                    WhereCurrentMouseClickStarted == ScreenMouseHoverKind.Inspector)
                {
                    Input.CursorType = MouseCursorType.Arrow;
                }
            }

            //else
            //{
            //    Input.CursorType = MouseCursorType.Arrow;
            //}

            //if (MouseHoverKind == ScreenMouseHoverKind.Inspector)
            //    Input.CursorType = MouseCursorType.StopUpdating;

            //if (editScreenGraphInspector.Rect.Contains(Input.MousePositionPoint))
            //    editScreenGraphInspector.Update(elapsedSeconds, allowMouseUpdate: CurrentDividerDragMode == DividerDragMode.None);
            //else
            //    editScreenGraphInspector.UpdateMouseOutsideRect(elapsedSeconds, allowMouseUpdate: CurrentDividerDragMode == DividerDragMode.None);

            oldMouseHoverKind = MouseHoverKind;
            
        }

        public void HandleWindowResize(Rectangle oldBounds, Rectangle newBounds)
        {
            if (oldBounds.Width > 0 && oldBounds.Height > 0 && newBounds.Width > 0 && newBounds.Height > 0)
            {
                float ratioW = 1.0f * newBounds.Width / oldBounds.Width;
                float ratioH = 1.0f * newBounds.Height / oldBounds.Height;

                LeftSectionWidth = LeftSectionWidth * ratioW;
                RightSectionWidth = RightSectionWidth * ratioW;
                TopRightPaneHeight = TopRightPaneHeight * ratioH;

                UpdateLayout();
            }
            
        }

        public void UpdateLayout()
        {
           

            if (Rect.IsEmpty)
            {
                return;
            }
                if (TopRightPaneHeight < TopRightPaneHeightMinNew)
                    TopRightPaneHeight = TopRightPaneHeightMinNew;




                if (RightSectionWidth < RightSectionWidthMin)
                    RightSectionWidth = RightSectionWidthMin;

                if (TopRightPaneHeight > (Rect.Height - BottomRightPaneHeightMinNew - TopMenuBarMargin - TransportHeight))
                {
                    TopRightPaneHeight = (Rect.Height - BottomRightPaneHeightMinNew - TopMenuBarMargin - TransportHeight);
                    Main.RequestViewportRenderTargetResolutionChange = true;
                    Main.RequestHideOSD = Main.RequestHideOSD_MAX;
                }

                if (AnimationListScreen != null && Graph != null)
                {
                    if (LeftSectionWidth < LeftSectionWidthMin)
                    {
                        LeftSectionWidth = LeftSectionWidthMin;
                        Main.RequestViewportRenderTargetResolutionChange = true;
                        Main.RequestHideOSD = Main.RequestHideOSD_MAX;
                    }


                    if (MiddleSectionWidth < MiddleSectionWidthMin)
                    {
                        var adjustment = MiddleSectionWidthMin - MiddleSectionWidth;
                        RightSectionWidth -= adjustment;
                        Main.RequestViewportRenderTargetResolutionChange = true;
                        Main.RequestHideOSD = Main.RequestHideOSD_MAX;
                    }

                    AnimationListScreen.Rect = new Rectangle(
                        (int)LeftSectionStartX,
                        Rect.Top + TopMenuBarMargin,
                        (int)LeftSectionWidth,
                        Rect.Height - TopMenuBarMargin);

                    Graph.Rect = new Rectangle(
                        (int)MiddleSectionStartX,
                        Rect.Top + TopMenuBarMargin + TopOfGraphAnimInfoMargin,
                        (int)MiddleSectionWidth,
                        Rect.Height - TopMenuBarMargin - TopOfGraphAnimInfoMargin);

                    var plannedGraphRect = new Rectangle(
                        (int)MiddleSectionStartX,
                        Rect.Top + TopMenuBarMargin + TopOfGraphAnimInfoMargin,
                        (int)MiddleSectionWidth,
                        Rect.Height - TopMenuBarMargin - TopOfGraphAnimInfoMargin);
                }
                else
                {
                    var plannedGraphRect = new Rectangle(
                        (int)MiddleSectionStartX,
                        Rect.Top + TopMenuBarMargin + TopOfGraphAnimInfoMargin,
                        (int)MiddleSectionWidth,
                        Rect.Height - TopMenuBarMargin - TopOfGraphAnimInfoMargin);
                }

                Transport.Rect = new Rectangle(
                        (int)RightSectionStartX,
                        Rect.Top + TopMenuBarMargin,
                        (int)RightSectionWidth,
                        (int)(TransportHeight));

                //editScreenGraphInspector.Rect = new Rectangle(Rect.Width - LayoutInspectorWidth, 0, LayoutInspectorWidth, Rect.Height);


                //inspectorWinFormsControl.Bounds = new System.Drawing.Rectangle((int)RightSectionStartX, Rect.Top + TopMenuBarMargin, (int)RightSectionWidth, (int)(Rect.Height - TopMenuBarMargin - BottomRightPaneHeight - DividerVisiblePad));
                //ModelViewerBounds = new Rectangle((int)RightSectionStartX, (int)(Rect.Bottom - BottomRightPaneHeight), (int)RightSectionWidth, (int)(BottomRightPaneHeight));

                //ShaderAdjuster.Size = new System.Drawing.Size((int)RightSectionWidth, ShaderAdjuster.Size.Height);
                ModelViewerBounds = new Rectangle(
                    (int)RightSectionStartX, 
                    Rect.Top + TopMenuBarMargin + TransportHeight, 
                    (int)RightSectionWidth, 
                    (int)(TopRightPaneHeight));
                ModelViewerBounds_InputArea = new Rectangle(
                    ModelViewerBounds.X + (DividerHitboxPad / 2),
                    ModelViewerBounds.Y + (DividerHitboxPad / 2),
                    ModelViewerBounds.Width - DividerHitboxPad,
                    ModelViewerBounds.Height - DividerHitboxPad);

                ImGuiEventInspectorPos = new System.Numerics.Vector2(RightSectionStartX, 
                    Rect.Top + TopMenuBarMargin + TopRightPaneHeight + DividerVisiblePad + TransportHeight);
                ImGuiEventInspectorSize = new System.Numerics.Vector2(RightSectionWidth, 
                    Rect.Height - TopRightPaneHeight - DividerVisiblePad - TopMenuBarMargin - TransportHeight);

                //ShaderAdjuster.Location = new System.Drawing.Point(Rect.Right - ShaderAdjuster.Size.Width, Rect.Top + TopMenuBarMargin);

           
        }

        public void DrawDimmingRect(GraphicsDevice gd, SpriteBatch sb, Texture2D boxTex)
        {
            sb.Begin(transformMatrix: Main.DPIMatrix);
            try
            {
                sb.Draw(boxTex, new Rectangle(Rect.Left, Rect.Top, (int)RightSectionStartX - Rect.X, Rect.Height), Color.Black * 0.25f);
            }
            finally { sb.End(); }
        }

        public void Draw(GraphicsDevice gd, SpriteBatch sb, Texture2D boxTex,
            SpriteFont font, float elapsedSeconds, SpriteFont smallFont, Texture2D scrollbarArrowTex)
        {

            sb.Begin();
            try
            {
                sb.Draw(boxTex, new Rectangle(Rect.X, Rect.Y, (int)RightSectionStartX - Rect.X, Rect.Height).DpiScaled(), Main.Colors.MainColorBackground);

                // Draw model viewer background lel
                //sb.Draw(boxTex, ModelViewerBounds, Color.Gray);

            }
            finally { sb.End(); }



            //throw new Exception("TaeUndoMan");

            //throw new Exception("Make left/right edges of events line up to same vertical lines so the rounding doesnt make them 1 pixel off");
            //throw new Exception("Make dragging edges of scrollbar box do zoom");
            //throw new Exception("make ctrl+scroll zoom centered on mouse cursor pos");

            UpdateLayout();

            if (AnimationListScreen != null)
            {
                AnimationListScreen.Draw(gd, sb, boxTex, font, scrollbarArrowTex);

                Rectangle curAnimInfoTextRect = new Rectangle(
                    (int)(MiddleSectionStartX),
                    Rect.Top + TopMenuBarMargin,
                    (int)(MiddleSectionWidth),
                    TopOfGraphAnimInfoMargin);

                sb.Begin();
                try
                {
                    if (Config.EnableFancyScrollingStrings)
                    {
                        SelectedTaeAnimInfoScrollingText.Draw(gd, sb, Main.DPIMatrix, curAnimInfoTextRect, font, elapsedSeconds, Main.GlobalTaeEditorFontOffset);
                    }
                    else
                    {
                        var curAnimInfoTextPos = curAnimInfoTextRect.Location.ToVector2();

                        sb.DrawString(font, SelectedTaeAnimInfoScrollingText.Text, curAnimInfoTextPos + Vector2.One + Main.GlobalTaeEditorFontOffset, Color.Black);
                        sb.DrawString(font, SelectedTaeAnimInfoScrollingText.Text, curAnimInfoTextPos + (Vector2.One * 2) + Main.GlobalTaeEditorFontOffset, Color.Black);
                        sb.DrawString(font, SelectedTaeAnimInfoScrollingText.Text, curAnimInfoTextPos + Main.GlobalTaeEditorFontOffset, Color.White);
                    }

                    //sb.DrawString(font, SelectedTaeAnimInfoScrollingText, curAnimInfoTextPos + Vector2.One, Color.Black);
                    //sb.DrawString(font, SelectedTaeAnimInfoScrollingText, curAnimInfoTextPos + (Vector2.One * 2), Color.Black);
                    //sb.DrawString(font, SelectedTaeAnimInfoScrollingText, curAnimInfoTextPos, Color.White);
                }
                finally { sb.End(); }



            }

            if (Graph != null)
            {
                Graph.Draw(gd, sb, boxTex, font, elapsedSeconds, smallFont, scrollbarArrowTex);


            }
            else
            {
                // Draws a very, very blank graph is none is loaded:

                //var graphRect = new Rectangle(
                //        (int)MiddleSectionStartX,
                //        Rect.Top + TopMenuBarMargin + TopOfGraphAnimInfoMargin,
                //        (int)MiddleSectionWidth,
                //        Rect.Height - TopMenuBarMargin - TopOfGraphAnimInfoMargin);

                //sb.Begin();
                //sb.Draw(texture: boxTex,
                //    position: new Vector2(graphRect.X, graphRect.Y),
                //    sourceRectangle: null,
                //    color: new Color(120, 120, 120, 255),
                //    rotation: 0,
                //    origin: Vector2.Zero,
                //    scale: new Vector2(graphRect.Width, graphRect.Height),
                //    effects: SpriteEffects.None,
                //    layerDepth: 0
                //    );

                //sb.Draw(texture: boxTex,
                //    position: new Vector2(graphRect.X, graphRect.Y),
                //    sourceRectangle: null,
                //    color: new Color(64, 64, 64, 255),
                //    rotation: 0,
                //    origin: Vector2.Zero,
                //    scale: new Vector2(graphRect.Width, TaeEditAnimEventGraph.TimeLineHeight),
                //    effects: SpriteEffects.None,
                //    layerDepth: 0
                //    );
                //sb.End();
            }

            Transport.Draw(gd, sb, boxTex, smallFont);

            if (AnimSwitchRenderCooldown > 0)
            {
                AnimSwitchRenderCooldown -= Main.DELTA_UPDATE;

                //float ratio = Math.Max(0, Math.Min(1, MathHelper.Lerp(0, 1, AnimSwitchRenderCooldown / AnimSwitchRenderCooldownFadeLength)));
                //sb.Begin();
                //sb.Draw(boxTex, graphRect, AnimSwitchRenderCooldownColor * ratio);
                //sb.End();
            }

            //editScreenGraphInspector.Draw(gd, sb, boxTex, font);

            //var oldViewport = gd.Viewport;
            //gd.Viewport = new Viewport(Rect.X, Rect.Y, Rect.Width, TopMargin);
            //{
            //    sb.Begin();

            //    sb.DrawString(font, $"{TaeFileName}", new Vector2(4, 4) + Vector2.One, Color.Black);
            //    sb.DrawString(font, $"{TaeFileName}", new Vector2(4, 4), Color.White);

            //    sb.End();
            //}
            //gd.Viewport = oldViewport;
            
        }
    }
}
