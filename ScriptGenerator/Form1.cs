﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using ScintillaNET;
using ScriptGenerator.Properties;
using ScriptGenerator.Utils.Providers;

namespace ScriptGenerator
{
    public partial class Form1 : Form
    {
        Scintilla TemplateTextArea;

        public Form1()
        {
            InitializeComponent();

            Settings.Default.SettingChanging += (sender, args) =>
            {
                if (args.SettingName == nameof(Settings.Default.TemplateFilePath))
                {
                    lblTemplateFilePath.Text = args.NewValue as string;
                }
            };
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var fdlg = new OpenFileDialog();
            fdlg.Title = "CSV Open File Dialog";
            fdlg.InitialDirectory = @"c:\";
            fdlg.Filter = string.Join("|", DataSourceHelper.LoadeDataSources.Select(x => x.FilterExtension));

            fdlg.RestoreDirectory = true;
            fdlg.InitialDirectory = Settings.Default.InputFolder;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                var fileToOpen = fdlg.FileName;

                txtInputFile.Text = fdlg.FileName;
                gvCSVPreview.DataSource = DataSourceHelper.FindByExtension(Path.GetExtension(fileToOpen)).LoadFromFile(fileToOpen);

                Settings.Default.InputFilePath = fdlg.FileName;
                Settings.Default.InputFolder = Path.GetDirectoryName(fdlg.FileName);
                Settings.Default.Save();
            }
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            if (PreValidate())
            {
                btnGenerate.Text = "Generating";
                var text = GenerateTemplate(true);
                btnGenerate.Text = "Generate";
                var saveFileDialog1 = new SaveFileDialog
                {
                    Filter = "SQL File|*.sql",
                    Title = "Save an SQL File"
                };
                saveFileDialog1.ShowDialog();
                
                File.WriteAllText(saveFileDialog1.FileName, text);

                MessageBox.Show("File has been saved to " + saveFileDialog1.FileName);
            }
        }

        private void LoadDataFromFile(string path)
        {
            if (File.Exists(path))
            {
                TemplateTextArea.Text = File.ReadAllText(path);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // CREATE CONTROL
            TemplateTextArea = new ScintillaNET.Scintilla();
            TextPanel.Controls.Add(TemplateTextArea);

            // BASIC CONFIG
            TemplateTextArea.Dock = System.Windows.Forms.DockStyle.Fill;
            TemplateTextArea.TextChanged += (this.OnTextChanged);

            // INITIAL VIEW CONFIG
            TemplateTextArea.WrapMode = WrapMode.None;
            TemplateTextArea.IndentationGuides = IndentView.LookBoth;

            // STYLING
            InitColors();
            InitSyntaxColoring();

            // NUMBER MARGIN
            InitNumberMargin();

            // BOOKMARK MARGIN
            InitBookmarkMargin();

            // CODE FOLDING MARGIN
            InitCodeFolding();

            // DRAG DROP
            InitDragDropFile();

            if (File.Exists(Settings.Default.TemplateFilePath))
            {
                LoadDataFromFile(Settings.Default.TemplateFilePath);
                lblTemplateFilePath.Text = Settings.Default.TemplateFilePath;
            }

            // INIT HOTKEYS
            InitHotkeys();

            if (File.Exists(Settings.Default.InputFilePath))
            {
                txtInputFile.Text = Settings.Default.InputFilePath;
            }

            if (File.Exists(Settings.Default.InputFilePath))
            {
                gvCSVPreview.DataSource = DataSourceHelper.FindByExtension(Path.GetExtension(Settings.Default.InputFilePath)).LoadFromFile(Settings.Default.InputFilePath);
            }
        }

        private void OnTextChanged(object sender, EventArgs e)
        {

        }

        #region Numbers, Bookmarks, Code Folding

        /// <summary>
        /// the background color of the text area
        /// </summary>
        private const int BACK_COLOR = 0x2A211C;

        /// <summary>
        /// default text color of the text area
        /// </summary>
        private const int FORE_COLOR = 0xB7B7B7;

        /// <summary>
        /// change this to whatever margin you want the line numbers to show in
        /// </summary>
        private const int NUMBER_MARGIN = 1;

        /// <summary>
        /// change this to whatever margin you want the bookmarks/breakpoints to show in
        /// </summary>
        private const int BOOKMARK_MARGIN = 2;

        private const int BOOKMARK_MARKER = 2;

        /// <summary>
        /// change this to whatever margin you want the code folding tree (+/-) to show in
        /// </summary>
        private const int FOLDING_MARGIN = 3;

        /// <summary>
        /// set this true to show circular buttons for code folding (the [+] and [-] buttons on the margin)
        /// </summary>
        private const bool CODEFOLDING_CIRCULAR = true;

        private void InitNumberMargin()
        {

            TemplateTextArea.Styles[Style.LineNumber].BackColor = IntToColor(BACK_COLOR);
            TemplateTextArea.Styles[Style.LineNumber].ForeColor = IntToColor(FORE_COLOR);
            TemplateTextArea.Styles[Style.IndentGuide].ForeColor = IntToColor(FORE_COLOR);
            TemplateTextArea.Styles[Style.IndentGuide].BackColor = IntToColor(BACK_COLOR);

            var nums = TemplateTextArea.Margins[NUMBER_MARGIN];
            nums.Width = 30;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;

            TemplateTextArea.MarginClick += TextArea_MarginClick;
        }

        private void InitBookmarkMargin()
        {

            //TextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));

            var margin = TemplateTextArea.Margins[BOOKMARK_MARGIN];
            margin.Width = 20;
            margin.Sensitive = true;
            margin.Type = MarginType.Symbol;
            margin.Mask = (1 << BOOKMARK_MARKER);
            //margin.Cursor = MarginCursor.Arrow;

            var marker = TemplateTextArea.Markers[BOOKMARK_MARKER];
            marker.Symbol = MarkerSymbol.Circle;
            marker.SetBackColor(IntToColor(0xFF003B));
            marker.SetForeColor(IntToColor(0x000000));
            marker.SetAlpha(100);

        }

        private void InitCodeFolding()
        {

            TemplateTextArea.SetFoldMarginColor(true, IntToColor(BACK_COLOR));
            TemplateTextArea.SetFoldMarginHighlightColor(true, IntToColor(BACK_COLOR));

            // Enable code folding
            TemplateTextArea.SetProperty("fold", "1");
            TemplateTextArea.SetProperty("fold.compact", "1");

            // Configure a margin to display folding symbols
            TemplateTextArea.Margins[FOLDING_MARGIN].Type = MarginType.Symbol;
            TemplateTextArea.Margins[FOLDING_MARGIN].Mask = Marker.MaskFolders;
            TemplateTextArea.Margins[FOLDING_MARGIN].Sensitive = true;
            TemplateTextArea.Margins[FOLDING_MARGIN].Width = 20;

            // Set colors for all folding markers
            for (var i = 25; i <= 31; i++)
            {
                TemplateTextArea.Markers[i].SetForeColor(IntToColor(BACK_COLOR)); // styles for [+] and [-]
                TemplateTextArea.Markers[i].SetBackColor(IntToColor(FORE_COLOR)); // styles for [+] and [-]
            }

            // Configure folding markers with respective symbols
            TemplateTextArea.Markers[Marker.Folder].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlus : MarkerSymbol.BoxPlus;
            TemplateTextArea.Markers[Marker.FolderOpen].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinus : MarkerSymbol.BoxMinus;
            TemplateTextArea.Markers[Marker.FolderEnd].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CirclePlusConnected : MarkerSymbol.BoxPlusConnected;
            TemplateTextArea.Markers[Marker.FolderMidTail].Symbol = MarkerSymbol.TCorner;
            TemplateTextArea.Markers[Marker.FolderOpenMid].Symbol = CODEFOLDING_CIRCULAR ? MarkerSymbol.CircleMinusConnected : MarkerSymbol.BoxMinusConnected;
            TemplateTextArea.Markers[Marker.FolderSub].Symbol = MarkerSymbol.VLine;
            TemplateTextArea.Markers[Marker.FolderTail].Symbol = MarkerSymbol.LCorner;

            // Enable automatic folding
            TemplateTextArea.AutomaticFold = (AutomaticFold.Show | AutomaticFold.Click | AutomaticFold.Change);

        }

        private void TextArea_MarginClick(object sender, MarginClickEventArgs e)
        {
            if (e.Margin == BOOKMARK_MARGIN)
            {
                // Do we have a marker for this line?
                const uint mask = (1 << BOOKMARK_MARKER);
                var line = TemplateTextArea.Lines[TemplateTextArea.LineFromPosition(e.Position)];
                if ((line.MarkerGet() & mask) > 0)
                {
                    // Remove existing bookmark
                    line.MarkerDelete(BOOKMARK_MARKER);
                }
                else
                {
                    // Add bookmark
                    line.MarkerAdd(BOOKMARK_MARKER);
                }
            }
        }

        #endregion

        #region Drag & Drop File

        public void InitDragDropFile()
        {

            TemplateTextArea.AllowDrop = true;
            TemplateTextArea.DragEnter += delegate(object sender, DragEventArgs e)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
                else
                    e.Effect = DragDropEffects.None;
            };
            TemplateTextArea.DragDrop += delegate(object sender, DragEventArgs e)
            {

                // get file drop
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {

                    var a = (Array) e.Data.GetData(DataFormats.FileDrop);
                    if (a != null)
                    {

                        var path = a.GetValue(0).ToString();

                        LoadDataFromFile(path);

                    }
                }
            };

        }

        #endregion

        private void InitColors()
        {

            TemplateTextArea.SetSelectionBackColor(true, IntToColor(0x114D9C));

        }

        private void InitHotkeys()
        {

            // register the hotkeys with the form
            //HotKeyManager.AddHotKey(this, OpenSearch, Keys.F, true);
            //HotKeyManager.AddHotKey(this, OpenFindDialog, Keys.F, true, false, true);
            //HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.R, true);
            //HotKeyManager.AddHotKey(this, OpenReplaceDialog, Keys.H, true);
            //HotKeyManager.AddHotKey(this, Uppercase, Keys.U, true);
            //HotKeyManager.AddHotKey(this, Lowercase, Keys.L, true);
            //HotKeyManager.AddHotKey(this, ZoomIn, Keys.Oemplus, true);
            //HotKeyManager.AddHotKey(this, ZoomOut, Keys.OemMinus, true);
            //HotKeyManager.AddHotKey(this, ZoomDefault, Keys.D0, true);
            //HotKeyManager.AddHotKey(this, CloseSearch, Keys.Escape);

            // remove conflicting hotkeys from scintilla
            TemplateTextArea.ClearCmdKey(Keys.Control | Keys.F);
            TemplateTextArea.ClearCmdKey(Keys.Control | Keys.R);
            TemplateTextArea.ClearCmdKey(Keys.Control | Keys.H);
            TemplateTextArea.ClearCmdKey(Keys.Control | Keys.L);
            TemplateTextArea.ClearCmdKey(Keys.Control | Keys.U);

        }

        private void InitSyntaxColoring()
        {
            // Configure the default style
            TemplateTextArea.StyleResetDefault();
            TemplateTextArea.Styles[Style.Default].Font = "Consolas";
            TemplateTextArea.Styles[Style.Default].Size = 10;
            TemplateTextArea.Styles[Style.Default].BackColor = IntToColor(0x212121);
            TemplateTextArea.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);
            TemplateTextArea.StyleClearAll();

            // Configure the CPP (C#) lexer styles
            //TextArea.Styles[Style.Cpp.Identifier].ForeColor = IntToColor(0xD0DAE2);
            //TextArea.Styles[Style.Cpp.Comment].ForeColor = IntToColor(0xBD758B);
            //TextArea.Styles[Style.Cpp.CommentLine].ForeColor = IntToColor(0x40BF57);
            //TextArea.Styles[Style.Cpp.CommentDoc].ForeColor = IntToColor(0x2FAE35);
            //TextArea.Styles[Style.Cpp.Number].ForeColor = IntToColor(0xFFFF00);
            //TextArea.Styles[Style.Cpp.String].ForeColor = IntToColor(0xFFFF00);
            //TextArea.Styles[Style.Cpp.Character].ForeColor = IntToColor(0xE95454);
            //TextArea.Styles[Style.Cpp.Preprocessor].ForeColor = IntToColor(0x8AAFEE);
            //TextArea.Styles[Style.Cpp.Operator].ForeColor = IntToColor(0xE0E0E0);
            //TextArea.Styles[Style.Cpp.Regex].ForeColor = IntToColor(0xff00ff);
            //TextArea.Styles[Style.Cpp.CommentLineDoc].ForeColor = IntToColor(0x77A7DB);
            //TextArea.Styles[Style.Cpp.Word].ForeColor = IntToColor(0x48A8EE);
            //TextArea.Styles[Style.Cpp.Word2].ForeColor = IntToColor(0xF98906);
            //TextArea.Styles[Style.Cpp.CommentDocKeyword].ForeColor = IntToColor(0xB3D991);
            //TextArea.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = IntToColor(0xFF0000);
            //TextArea.Styles[Style.Cpp.GlobalClass].ForeColor = IntToColor(0x48A8EE);
            TemplateTextArea.Styles[Style.Sql.Word].ForeColor = Color.FromArgb(147, 199, 99);
            TemplateTextArea.Styles[Style.Sql.Word].Bold = true;
            TemplateTextArea.Styles[Style.Sql.Identifier].ForeColor = Color.FromArgb(255, 255, 255);
            TemplateTextArea.Styles[Style.Sql.Character].ForeColor = Color.FromArgb(236, 118, 0);
            TemplateTextArea.Styles[Style.Sql.Number].ForeColor = Color.FromArgb(255, 205, 34);
            TemplateTextArea.Styles[Style.Sql.Operator].ForeColor = Color.FromArgb(232, 226, 183);
            TemplateTextArea.Styles[Style.Sql.Comment].ForeColor = Color.FromArgb(102, 116, 123);
            TemplateTextArea.Styles[Style.Sql.CommentLine].ForeColor = Color.FromArgb(102, 116, 123);
            TemplateTextArea.Lexer = Lexer.Sql;

            TemplateTextArea.SetKeywords(0, "class extends implements import interface new case do while else if for in switch throw get set function var try catch finally while with default break continue delete return each const namespace package include use is as instanceof typeof author copy default deprecated eventType example exampleText exception haxe inheritDoc internal link mtasc mxmlc param private return see serial serialData serialField since throws usage version langversion playerversion productversion dynamic private public partial static intrinsic internal native override protected AS3 final super this arguments null Infinity NaN undefined true false abstract as base bool break by byte case catch char checked class const continue decimal default delegate do double descending explicit event extern else enum false finally fixed float for foreach from goto group if implicit in int interface internal into is lock long new null namespace object operator out override orderby params private protected public readonly ref return switch struct sbyte sealed short sizeof stackalloc static string select this throw true try typeof uint ulong unchecked unsafe ushort using var virtual volatile void while where yield");
            TemplateTextArea.SetKeywords(1, "void Null ArgumentError arguments Array Boolean Class Date DefinitionError Error EvalError Function int Math Namespace Number Object RangeError ReferenceError RegExp SecurityError String SyntaxError TypeError uint XML XMLList Boolean Byte Char DateTime Decimal Double Int16 Int32 Int64 IntPtr SByte Single UInt16 UInt32 UInt64 UIntPtr Void Path File System Windows Forms ScintillaNET");



        }

        #region Utils

        public static Color IntToColor(int rgb)
        {
            return Color.FromArgb(255, (byte) (rgb >> 16), (byte) (rgb >> 8), (byte) rgb);
        }

        public void InvokeIfNeeded(Action action)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(action);
            }
            else
            {
                action.Invoke();
            }
        }

        #endregion

        private void btnSaveTemplate_Click(object sender, EventArgs e)
        {
            string filePath = Settings.Default.TemplateFilePath;

            if (!File.Exists(Settings.Default.TemplateFilePath))
            {
                var saveFileDialog1 = new SaveFileDialog
                {
                    Filter = "SQL File|*.sql",
                    Title = "Save an SQL File"
                };
                saveFileDialog1.ShowDialog();

                filePath = saveFileDialog1.FileName;
            }

            if (File.Exists(filePath))
            {
                File.WriteAllText(filePath, TemplateTextArea.Text);
            }
        }

        private void btnLoadTemplate_Click(object sender, EventArgs e)
        {
            var fdlg = new OpenFileDialog();
            fdlg.Title = "Save template File";
            fdlg.InitialDirectory = @"c:\";
            fdlg.Filter = "Text Files (*.txt)|*.txt|Sql Files (*.sql)|*.sql";
            
            fdlg.RestoreDirectory = true;
            fdlg.InitialDirectory = Settings.Default.TemplateFolder;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                LoadDataFromFile(fdlg.FileName);
                Settings.Default.TemplateFilePath = fdlg.FileName;
                Settings.Default.TemplateFolder = Path.GetDirectoryName(fdlg.FileName);
                Settings.Default.Save();
            }
        }

        private bool PreValidate()
        {
            if (!File.Exists(txtInputFile.Text))
            {
                MessageBox.Show("Input file File",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }
            if (TemplateTextArea.Text.Length == 0)
            {
                MessageBox.Show("Template is required",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return false;
            }

            return true;
        }

        private string GenerateTemplate(bool all = false)
        {
            var data = DataSourceHelper.FindByExtension(Path.GetExtension(txtInputFile.Text)).LoadFromFile(txtInputFile.Text, all ? int.MaxValue : 10);

            var st = new StringBuilder();

            foreach (DataRow dataRow in data.Rows)
            {
                var dict = dataRow.Table.Columns
                    .Cast<DataColumn>()
                    .ToDictionary(c => c.ColumnName, c => dataRow[c]);


                var text = SmartFormat.Smart.Format(TemplateTextArea.Text, dict);

                st.AppendLine(text);
                st.AppendLine();
            }

            return st.ToString();
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            PreviewForm = PreviewForm ?? new PreviewForm {ParentForm = this};
            PreviewForm.Show();
            Hide();

            if (PreValidate())
            {
                PreviewForm.SetText(GenerateTemplate());
            }
        }

        public PreviewForm PreviewForm { get; set; }

        private void btnCreateScript_Click(object sender, EventArgs e)
        {
            TemplateTextArea.Text = SqlHelper.CreateTable(Path.GetFileNameWithoutExtension(txtInputFile.Text), DataSourceHelper.FindByExtension(txtInputFile.Text).LoadFromFile(txtInputFile.Text));
        }
    }


}
