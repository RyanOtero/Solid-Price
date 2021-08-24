﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Solid_Price.Models {
    public class CutItem : IComparable<CutItem>, IEquatable<CutItem>, INotifyPropertyChanged {

        #region Fields
        private float length;
        private float angle1;
        private float angle2;
        private string angleDirection;
        private string angleRotation;
        private StockItem stockItem;
        private int stockItemID;
        private int qty;
        private int stickNumber;
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Properties
        public int ID { get; set; }

        [ForeignKey("StockItemID")]
        public int StockItemID {
            get => stockItemID;
            set {
                stockItemID = value;
                OnPropertyChanged();
            }
        }


        public StockItem StockItem {
            get => stockItem;
            set {
                if (value != null) StockItemID = value.ID;
                stockItem = value;
                OnPropertyChanged();
            }
        }

        public int Qty {
            get => qty;
            set {
                qty = value;
                OnPropertyChanged();
            }
        }

        public string Description {
            get {return StockItem?.ExternalDescription; }
            //get => description;
            //set {
            //    description = value;
            //    OnPropertyChanged();
            //}
        }

        public float Length {
            get => length;
            set {
                length = value;
                OnPropertyChanged();
            }
        }

        public float Angle1 {
            get => angle1;
            set {
                angle1 = value;
                OnPropertyChanged();
            }
        }

        public float Angle2 {
            get => angle2;
            set {
                angle2 = value;
                OnPropertyChanged();
            }
        }

        public string AngleDirection {
            get => angleDirection;
            set {
                angleDirection = value;
                OnPropertyChanged();
            }
        }

        public string AngleRotation {
            get => angleRotation;
            set {
                angleRotation = value;
                OnPropertyChanged();
            }
        }

        public int StickNumber {
            get => stickNumber;
            set {
                stickNumber = value;
                OnPropertyChanged();
            }
        }

        public string Cost {
            get {
                decimal num = StockItem != null ? StockItem.CostPerFoot : 0;
                return string.Format("{0:c}", num / 12 * (decimal)Length);
            }
            //get => cost;
            //set {
            //    cost = value;
            //    OnPropertyChanged();
            //}
        }

        public string TotalCost {
            get {
                decimal num = StockItem != null ? StockItem.CostPerFoot : 0;
                return string.Format("{0:c}", num / 12 * (decimal)Length * Qty);
            }        //    get => totalCost;
                     //    set {
                     //        totalCost = value;
                     //        OnPropertyChanged();
                     //    }
        }

        public string StickNumberString {
            get {
                if (StickNumber == 0) {
                    return "-";
                }
                return StickNumber.ToString();
            }
        }
        #endregion


        public CutItem() { }

        public CutItem(StockItem stockItem, int qty, float length, float angle1, float angle2, string angleDirection, string angleRotation, int stickNumber = 0) {
            StockItem = stockItem;
            StockItemID = stockItem.ID;
            //Cost = string.Format("{0:c}", stockItem.CostPerFoot / 12m * (decimal)Length);
            //TotalCost = string.Format("{0:c}", StockItem.CostPerFoot / 12m * (decimal)Length * Qty);
            //Description = stockItem.ExternalDescription;
            Qty = qty;
            Length = length;
            Angle1 = angle1;
            Angle2 = angle2;
            AngleDirection = angleDirection;
            AngleRotation = angleRotation;
            StickNumber = stickNumber;
        }


        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int CompareTo(CutItem other) {
            int i = Description.CompareTo(other.Description);
            if (i == -1) {
                return -1;
            } else if (i == 1) {
                return 1;
            } else {
                int j = StickNumber.CompareTo(other.StickNumber);
                if (j == 1) {
                    return 1;
                } else if (j == -1) {
                    return -1;
                } else {
                    int k = Length.CompareTo(other.Length);
                    if (k == -1) {
                        return 1;
                    } else if (k == 1) {
                        return -1;
                    } else {
                        return 0;
                    }
                }
            }

        }

        public bool Equals(CutItem other) {
            if (other.Length == Length &&
                other.Description == Description &&
                other.StockItem == StockItem &&
                other.Qty == Qty &&
                other.Angle1 == Angle1 &&
                other.Angle2 == Angle2 &&
                other.AngleDirection == AngleDirection &&
                other.AngleRotation == AngleRotation) {
                return true;
            }
            return false;
        }

        public CutItem Clone() {
            return new CutItem(StockItem, Qty, Length, Angle1, Angle2, AngleDirection, AngleRotation, StickNumber);
        }
    }
}