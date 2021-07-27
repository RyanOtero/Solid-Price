﻿using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using Solidworks_Cutlist_Generator.Model;
using Solidworks_Cutlist_Generator.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static Solidworks_Cutlist_Generator.BusinessLogic.Messenger;


namespace Solidworks_Cutlist_Generator.BusinessLogic {
    class CutListMaker {
        public SldWorks swApp;
        int fileerror = 0;
        int filewarning = 0;
        bool inBodyFolder = false;
        private ObservableCollection<CutItem> cutList;
        private readonly object cutListLock;

        public ObservableCollection<CutItem> CutList {
            get => cutList; set {
                cutList = value;
                BindingOperations.EnableCollectionSynchronization(cutList, cutListLock);
            }
        }

        public CutListMaker(ObservableCollection<CutItem> cList) {
            cutListLock = new object();
            CutList = cList;
        }
        public void NewCutList() {
            CutList.Clear();
        }

        public void Generate(string filePath, bool isDetailed) {
            bool isPart;
            bool isAssembly;

            inBodyFolder = false;
            NewCutList();
            List<CutItem> tempList = new List<CutItem>();
            isPart = filePath.ToLower().Contains(".sldprt");
            isAssembly = filePath.ToLower().Contains(".sldasm");

            if (!isPart && !isAssembly) {
                ErrorMessage("Invalid File", "Please select a part file or an assembly file.");
            }
            var progId = "SldWorks.Application";
            var progType = Type.GetTypeFromProgID(progId);
            swApp = new SldWorks();
            swApp.Visible = true;
            // increase performance 
            ModelDoc2 doc;

            doc = isAssembly ?
                swApp.OpenDoc6(filePath, (int)swDocumentTypes_e.swDocASSEMBLY, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref fileerror, ref filewarning) :
                swApp.OpenDoc6(filePath, (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref fileerror, ref filewarning);

            // Set the working directory to the document directory
            swApp.SetCurrentWorkingDirectory(doc.GetPathName().Substring(0, doc.GetPathName().LastIndexOf("\\")));

            swApp.CommandInProgress = true;
            ModelDoc2 swModel;
            AssemblyDoc swAssy = default(AssemblyDoc);
            PartDoc swPart = default(PartDoc);
            swModel = swApp.ActiveDoc as ModelDoc2;
            if (isAssembly) {
                swAssy = (AssemblyDoc)swModel;
            } else {
                swPart = (PartDoc)swModel;
            }

            if (isAssembly) {
                TraverseAssembly(tempList, swAssy);
            } else {
                TraverseFeatures(tempList, (Feature)swPart.FirstFeature(), true, "Root Feature");
            }
            while (swApp.ActiveDoc != null) {
                swApp.CloseDoc(((ModelDoc2)swApp.ActiveDoc).GetPathName());
            }
            if (isDetailed) {
                SortCuts(tempList);
                foreach (CutItem item in tempList) {
                    CutList.Add(item);
                }
                return;
            }
            Consolidate(tempList);
            foreach (CutItem item in tempList) {
                CutList.Add(item);
            }
        }

        public void SortCuts(List<CutItem> cutList) {
            List<CutItem> temp = new List<CutItem>();
            StockItem sType = null;
            float leftOnStick = 0;
            int stickNum = 1;

            foreach (CutItem item in cutList) {
                for (int i = 0; i < item.Qty; i++) {
                    temp.Add(item.Clone());
                }
            }
            cutList.Clear();
            foreach (CutItem item in temp) {
                cutList.Add(item);
            }
            temp.Clear();
            cutList.Sort();
            cutList.Reverse();
            int count = cutList.Count;

            for (int i = count - 1; i > -1; i--) {
                //First item initialization
                if (i == count - 1) {
                    sType = cutList[i].StockType;
                    cutList[i].StickNumber = stickNum;
                    leftOnStick = cutList[i].StockType.StockLengthInInches - cutList[i].Length;
                    bool isLarger = false;
                    while (leftOnStick <= 0) {
                        stickNum++;
                        leftOnStick += cutList[i].StockType.StockLengthInInches;
                        isLarger = true;
                    }
                    if (isLarger) stickNum++;
                    temp.Add(cutList[i]);
                    cutList.RemoveAt(i);
                    //If still same stock type as previous
                } else if (cutList[i].StockType == sType) {
                    //If there is enough left on the current stick
                    if (leftOnStick >= cutList[i].Length) {
                        leftOnStick -= cutList[i].Length;
                        cutList[i].StickNumber = stickNum;
                        if (leftOnStick == 0) {
                            stickNum++;
                            leftOnStick = cutList[i].StockType.StockLengthInInches;
                        }
                        temp.Add(cutList[i]);
                        cutList.RemoveAt(i);
                        //If the piece is longer than the stock length
                    } else if (cutList[i].Length > sType.StockLengthInInches) {
                        sType = cutList[i].StockType;
                        leftOnStick = cutList[i].StockType.StockLengthInInches - cutList[i].Length;
                        cutList[i].StickNumber = stickNum;
                        bool isLarger = false;
                        while (leftOnStick <= 0) {
                            stickNum++;
                            leftOnStick += cutList[i].StockType.StockLengthInInches;
                            isLarger = true;
                        }
                        if (isLarger) stickNum++;
                        temp.Add(cutList[i]);
                        cutList.RemoveAt(i);
                        //If the piece is longer than current stick
                    } else {
                        bool isBroken = false;
                        bool areMoreOfStockType = false;
                        //check what other cuts may fit on the current stick and remove them
                        for (int j = i - 1; j > -1; j--) {
                            if (cutList[j].StockType == sType) {
                                areMoreOfStockType = true;
                                if (leftOnStick >= cutList[j].Length) {
                                    leftOnStick -= cutList[j].Length;
                                    cutList[j].StickNumber = stickNum;
                                    if (leftOnStick == 0) {
                                        stickNum++;
                                        leftOnStick = cutList[i].StockType.StockLengthInInches;
                                    }
                                    temp.Add(cutList[j]);
                                    cutList.RemoveAt(j);
                                    isBroken = true;
                                }
                            }
                        }
                        //if no other cuts will fit on current stick, go to full stock length and retry current iteration
                        if (!isBroken && areMoreOfStockType) {
                            leftOnStick = cutList[i].StockType.StockLengthInInches;
                            stickNum++;
                            i++;
                            continue;
                            //if other cuts fit on current stick, retry current iteration
                        } else if (isBroken) {
                            i++;
                            continue;
                            //if no more cuts fit on the stick
                        } else {
                            stickNum++;
                            cutList[i].StickNumber = stickNum;
                            temp.Add(cutList[i]);
                            cutList.RemoveAt(i);
                            continue;
                        }
                    }
                    // If new stocktype
                } else {
                    sType = cutList[i].StockType;
                    stickNum = 1;
                    leftOnStick = cutList[i].StockType.StockLengthInInches - cutList[i].Length;
                    cutList[i].StickNumber = stickNum;
                    bool isLarger = false;
                    while (leftOnStick <= 0) {
                        stickNum++;
                        leftOnStick += cutList[i].StockType.StockLengthInInches;
                        isLarger = true;
                    }
                    if (isLarger) stickNum++;
                    temp.Add(cutList[i]);
                    cutList.RemoveAt(i);
                }
            }
            Consolidate(temp);
            cutList.Clear();
            foreach (CutItem item in temp) {
                cutList.Add(item);
            }
        }

        public void Consolidate(List<CutItem> cList) {
            for (int i = cList.Count - 1; i > -1; i--) {
                for (int j = i - 1; j > -1; j--) {
                    if (cList[i].Description == cList[j].Description &&
                        cList[i].Length == cList[j].Length &&
                        cList[i].Angle1 == cList[j].Angle1 &&
                        cList[i].Angle2 == cList[j].Angle2 &&
                        cList[i].StickNumber == cList[j].StickNumber) {
                        cList[j].Qty += cList[i].Qty;
                        cList.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void AddCutItem(List<CutItem> cutList, Feature thisFeat) {
            CustomPropertyManager CustomPropMgr = default(CustomPropertyManager);
            CustomPropMgr = thisFeat.CustomPropertyManager;
            string[] vCustomPropNames;
            vCustomPropNames = (string[])CustomPropMgr.GetNames();
            if ((vCustomPropNames != null)) {
                int i = 0;
                int qty = 0;
                float length = 0;
                float angle1 = 0;
                float angle2 = 0;
                string angleDirection = "";
                string angleRotation = "";
                string description = "";
                string material = "";
                bool isNew = false;
                StockItem sItem = null;

                using (var ctx = new CutListGeneratorContext()) {
                    try {
                        for (i = 0; i <= (vCustomPropNames.Length - 1); i++) {
                            string CustomPropName = (string)vCustomPropNames[i];
                            string CustomPropResolvedVal;
                            CustomPropMgr.Get2(CustomPropName, out _, out CustomPropResolvedVal);
                            switch (CustomPropName.ToLower()) {
                                case "quantity":
                                    Int32.TryParse(CustomPropResolvedVal, out qty);
                                    break;
                                case "description":
                                    description = CustomPropResolvedVal;
                                    sItem = ctx.StockItems.Where(item => item.InternalDescription == description).FirstOrDefault(); ;
                                    if (sItem == null) {
                                        isNew = true;
                                    }
                                    break;
                                case "length":
                                    float.TryParse(CustomPropResolvedVal, out length);
                                    break;
                                case "angle1":
                                    float.TryParse(CustomPropResolvedVal.Substring(0, CustomPropResolvedVal.Length - 1), out angle1);
                                    break;
                                case "angle2":
                                    float.TryParse(CustomPropResolvedVal.Substring(0, CustomPropResolvedVal.Length - 1), out angle2);
                                    break;
                                case "material":
                                    material = CustomPropResolvedVal;
                                    break;
                                case "angle direction":
                                    angleDirection = CustomPropResolvedVal;
                                    break;
                                case "angle rotation":
                                    angleRotation = CustomPropResolvedVal;
                                    break;
                                default:
                                    break;
                            }
                        }
                        Vendor vendor;
                        if (isNew) {
                            if (ctx.Vendors.Any()) {
                                vendor = ctx.Vendors.AsEnumerable().ElementAt(0);
                            } else {
                                vendor = new Vendor("NULL", "NULL", "NULL", "NULL");
                                ctx.Vendors.Add(vendor);
                                ctx.SaveChanges();
                            }
                            sItem = new StockItem(vendor: vendor, internalDescription: description,
                                externalDescription: description,
                                materialType: StockItem.MaterialFromDescription(description),
                                profType: StockItem.ProfileFromDescription(description));
                            ctx.StockItems.Add(sItem);
                            ctx.SaveChanges();
                        }
                        isNew = false;

                        CutItem cItem = new CutItem(sItem, qty, length, angle1, angle2, angleDirection, angleRotation);
                        cutList.Add(cItem);
                    } catch (Exception) {
                        ErrorMessage("Database Error", "Cannot access database. Please check that it exists");
                    }

                }
            }
        }

        public void FindCutlist(List<CutItem> cutList, Feature thisFeat, string ParentName) {
            int BodyCount = 0;

            string FeatType = null;
            FeatType = thisFeat.GetTypeName();
            if ((FeatType == "SolidBodyFolder") & (ParentName == "Root Feature")) {
                inBodyFolder = true;
            }
            if ((FeatType != "SolidBodyFolder") & (ParentName == "Root Feature")) {
                inBodyFolder = false;
            }

            //Only consider the CutListFolders that are under SolidBodyFolder
            if ((inBodyFolder == false) & (FeatType == "CutListFolder")) {
                return;
            }

            //Only consider the SubWeldFolder that are under the SolidBodyFolder
            if ((inBodyFolder == false) & (FeatType == "SubWeldFolder")) {
                //Skip the second occurrence of the SubWeldFolders during the feature traversal
                return;
            }

            bool IsBodyFolder;
            if (FeatType == "SolidBodyFolder" | FeatType == "SurfaceBodyFolder" | FeatType == "CutListFolder" | FeatType == "SubWeldFolder" | FeatType == "SubAtomFolder") {
                IsBodyFolder = true;
            } else {
                IsBodyFolder = false;
            }

            if (IsBodyFolder) {
                BodyFolder BodyFolder = default(BodyFolder);
                BodyFolder = (BodyFolder)thisFeat.GetSpecificFeature2();
                BodyCount = BodyFolder.GetBodyCount();
                if ((FeatType == "CutListFolder") & (BodyCount < 1)) {
                    //When BodyCount = 0, this cut list folder is not displayed in the
                    //FeatureManager design tree, so skip it
                    return;
                }
            }

            if (FeatType == "CutListFolder") {
                if (BodyCount > 0) {
                    AddCutItem(cutList, thisFeat);
                }
            }
        }

        public void TraverseComponent(List<CutItem> tempList, Component2 swComp, Action<List<CutItem>, Feature, bool, string> action) {
            object[] vChildComp;
            Component2 swChildComp;

            vChildComp = (object[])swComp.GetChildren();
            if (vChildComp.Length > 0) {
                for (int i = 0; i < vChildComp.Length; i++) {
                    swChildComp = (Component2)vChildComp[i];
                    TraverseComponent(tempList, swChildComp, action);
                }
            } else {

                ModelDoc2 model = swApp.OpenDoc6(swComp.GetPathName(), (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref fileerror, ref filewarning); ;
                Feature feature = (Feature)model.FirstFeature();
                action(tempList, feature, true, "Root Feature");
            }
        }

        public void TraverseAssembly(List<CutItem> tempList, AssemblyDoc swAssy) {
            object[] vChildComp;
            Component2 swChildComp;

            vChildComp = (object[])swAssy.GetComponents(false);
            for (int i = 0; i < vChildComp.Length; i++) {
                swChildComp = (Component2)vChildComp[i];
                if (swChildComp.GetSuppression() == 2) {
                    ModelDoc2 model = (ModelDoc2)swChildComp.GetModelDoc2();
                    Feature feat = (Feature)model.FirstFeature();
                    TraverseComponent(tempList, swChildComp, TraverseFeatures);
                }
            }

        }

        public void TraverseFeatures(List<CutItem> tempList, Feature thisFeat, bool isTopLevel, string ParentName) {
            Feature curFeat = default(Feature);
            curFeat = thisFeat;
            while ((curFeat != null)) {
                if (tempList != null) {
                    FindCutlist(tempList, curFeat, ParentName);
                }
                Feature subfeat = default(Feature);
                subfeat = (Feature)curFeat.GetFirstSubFeature();
                while ((subfeat != null)) {
                    TraverseFeatures(tempList, subfeat, false, curFeat.Name);
                    Feature nextSubFeat = default(Feature);
                    nextSubFeat = (Feature)subfeat.GetNextSubFeature();
                    subfeat = (Feature)nextSubFeat;
                    nextSubFeat = null;
                }
                subfeat = null;
                Feature nextFeat = default(Feature);
                if (isTopLevel) {
                    nextFeat = (Feature)curFeat.GetNextFeature();
                } else {
                    nextFeat = null;
                }
                curFeat = (Feature)nextFeat;
            }
        }
    }
}