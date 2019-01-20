﻿/*
   Copyright 2016-2018 LIPtoH <liptoh.codebase@gmail.com>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlServerCe;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Deployment.Application;


namespace TS_SE_Tool
{
    public partial class FormMain : Form
    {
        #region  Accesslevels

        private int SavefileVersion;
        internal int SupportedSavefileVersionETS2;
        internal string SupportedGameVersionETS2;
        //internal int SupportedSavefileVersionATS;
        internal string SupportedGameVersionATS;

        private int InGameTime;
        private int JobsTotalDistance;
        private int JobsAmountAdded;

        private double ProgPrevVersion;

        private bool FileDecoded;

        private string GameType;
        private string SavefilePath;
        private string LastVisitedCity;
        private string LoopStartCity;
        private string LoopStartCompany;

        //private string UserCompanyAssignedTruck;
        //private string UserCompanyAssignedTrailer;
        //private string UserCompanyAssignedTruckPlacement;
        private bool UserCompanyAssignedTruckPlacementEdited;

        private string ProfileETS2;
        private string ProfileATS;

        private string[] CountryDictionaryFile;
        private string[] tempInfoFileInMemory;
        private string[] tempSavefileInMemory;
        private string[] tempProfileFileInMemory;
        private string[] JobsListAdded;
        private string[] CitiesListAddedToCompare;
        private string[] ListSavefileCompanysString;
        private string[] EconomyEventUnitLinkStringList;
        private string[] EconomyEventQueueList;

        private List<LevelNames> PlayerLevelNames;

        private string[,] EconomyEventsTable;

        private List<City> CitiesList;
        private List<string> CitiesListDB;
        private List<string> CitiesListDiff;

        private List<Cargo> CargoesList;
        private List<Cargo> CargoesListDB;
        private List<Cargo> CargoesListDiff;

        private List<string> HeavyCargoList;

        private List<string> CompaniesList;
        private List<string> CompaniesListDB;
        private List<string> CompaniesListDiff;

        private List<string> CountriesList;

        private List<Garages> GaragesList;
        private List<VisitedCity> VisitedCities;

        private List<CompanyTruck> CompanyTruckList;
        private List<CompanyTruck> CompanyTruckListDB;
        private List<CompanyTruck> CompanyTruckListDiff;

        private List<ExtCompany> ExternalCompanies;

        internal List<Color> UserColorsList;

        private SqlCeConnection DBconnection;

        private DateTime LastModifiedTimestamp;

        private PlayerProfile PlayerProfileData;

        internal ProgSettings ProgSettingsV;

        private Random RandomValue;

        private CountryDictionary CountryDictionary;

        private Routes RouteList;

        private Dictionary<string, string> dictionaryProfiles;
        private Dictionary<string, string> CompaniesLngDict, CargoLngDict, TruckBrandsLngDict;
        public static Dictionary<string, string> CitiesLngDict;
        //private Dictionary<string, UserCompanyTruck> UserTruckList;
        private Dictionary<string, UserCompanyTruckData> UserTruckDictionary;
        private Dictionary<string, UserCompanyTruckData> UserTrailerDictionary;

        private List<string> namelessList;
        private string namelessLast;

        private Dictionary<string, List<string>> GPSbehind, GPSahead, GPSbehindOnline, GPSaheadOnline;

        internal Dictionary<string, double> DistanceMultipliers;

        private DataTable DistancesTable;

        private Bitmap ProgressBarGradient;
        private Image RepairImg, RefuelImg;
        private Image[] ADRImgS, ADRImgSGrey, SkillImgSBG, SkillImgS, GaragesImg, CitiesImg, UrgencyImg, CargoTypeImg, TruckPartsImg, TrailerPartsImg, GameIconeImg;

        private ImageList TabpagesImages;

        private CheckBox[,] SkillButtonArray;
        private CheckBox[] ADRbuttonArray;

        internal double DistanceMultiplier = 1;
        private double km_to_mileconvert = 0.621371;

        #endregion

        public FormMain()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.MainIco;

            //buttonGameETS.Enabled = true;
            //buttonGameATS.Enabled = false;
            GetTranslationFiles();

            SetDefaultValues(true);
            LoadConfig();
            LoadExtCountries();
            LoadCompaniesLng();
            LoadCitiesLng();
            LoadCargoLng();
            LoadTruckBrandsLng();
            ChangeLanguage();
            ToggleVisibility(false);

            ToggleGame(GameType);
            LoadExtImages();
            //GetExternalCompaniesCargoInOut()

            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = false;
            worker.DoWork += CacheExternalCargoData;
            worker.RunWorkerAsync();

            CreateProfilePanelControls();
            CreateProgressBarBitmap();
            CreateTruckPanelControls();
            CreateTrailerPanelControls();

            tabControlMain.ImageList = TabpagesImages;

            for (int i = 0; i < TabpagesImages.Images.Count; i++)
            {
                tabControlMain.TabPages[i].ImageIndex = i;
            }

            listBoxFreightMarketAddedJobs.DrawMode = DrawMode.OwnerDrawVariable;
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            FillAllProfilesPaths();
            //FillProfiles();
        }

        private void buttonRefreshAll_Click(object sender, EventArgs e)
        {
            FillAllProfilesPaths();
            //FillProfiles();
            //FillProfileSaves();
            buttonMainDecryptSave.Enabled = true;
            buttonMainLoadSave.Enabled = true;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {

        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSettings FormWindow = new FormSettings();
            FormWindow.ShowDialog();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult exitDR = DialogResult.Yes;

            if (JobsListAdded != null && JobsListAdded.Length > 0)
                exitDR = MessageBox.Show("You have unsaved changes. Do you realy want to close down application?", "Close Application without saving changes", MessageBoxButtons.YesNo);
            else
                exitDR = MessageBox.Show("Do you realy want to close down application?", "Close Application", MessageBoxButtons.YesNo);

            if (exitDR == DialogResult.Yes)
            {
                WriteConfig();
            }
            else
            {
                e.Cancel = true;
                Activate();
            }
        }

        private void makeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportFormControlstoLanguageFile();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutWindow = new AboutBox();
            aboutWindow.ShowDialog();
        }
    }

    public class Globals
    {
        public static string[] ProfilesPaths;
        public static string[] ProfilesHex;
        public static string[] SavesHex;
        public static string CurrentGame = "";
        public static string ProfileSii = "";
        public static int[] PlayerLevelUps;
    }

}
