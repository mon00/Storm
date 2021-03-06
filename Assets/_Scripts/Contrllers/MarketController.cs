﻿using admob;
using Sdkbox;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;

namespace Controller
{
    public class MarketController : MonoBehaviour
    {
        #region Instance

        private static MarketController _instance = null;

        public static MarketController Instance
        {
            get
            {
                return _instance;
            }
        }

        private void Init()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(this);
            }
        }

        #endregion Instance

        private const int COST_10 = 10;
        private const int COST_FULL = 20;

        public delegate void SimpleVoid();

        public event SimpleVoid OnPortmoneChanged;

        public event SimpleVoid OnEnergyChanged;

        public event SimpleVoid OnMoneyChanged;

        public const float DoubleEnergyCosts = 1.5f;
        public const float HalfRestCost = 1.5f;
        public const float NoADCost = 3;
        public const float UnlimitedEnergyCost = 3f;
        public const float FullRestCost = 1f;

        public Portmone PMone { get; private set; }

        public int MaxEnergy
        {
            get
            {
                if (PMone.DoubleEnergy)
                    return Portmone.MAX_ENERGY * 2;
                return Portmone.MAX_ENERGY;
            }
        }

        private void Awake()
        {
            Init();

            PMone = Portmone.Load();
            if (PMone == null)
            {
                PMone = new Portmone
                {
                    Energy = Portmone.MAX_ENERGY,
                    Money = 0,
                    LastRest = DateTime.Now,
                };

                Save();
            }

            GameController.Instance.OnPlayerDestroy += MinusEnergyOnDestroy;
        }

        private void Start()
        {
            // For debug!

            //PMone.Energy = 1;
            //Save();

            //
        }

        private void OnDestroy()
        {
            GameController.Instance.OnPlayerDestroy -= MinusEnergyOnDestroy;
        }

        private void MinusEnergyOnDestroy()
        {
            MinusEnergy();
        }

        public void ShowRewardMessage()
        {
            Admob.Instance().loadRewardedVideo("ca-app-pub-9869209397937230/3019809509");
        }

        private System.Collections.IEnumerator WaitForReward()
        {
            int max = 240;
            while (!Admob.Instance().isRewardedVideoReady() && max > 0)
            {
                max--;
                yield return new WaitForFixedUpdate();
            }

            if (max > 0)
            {
                Admob.Instance().showRewardedVideo();
                Admob.Instance().rewardedVideoEventHandler += RevardViderAdd;
            }
        }

        private void RevardViderAdd(string eventName, string msg)
        {
            AddEnergy(2);
        }

        public bool MinusEnergy()
        {
            return MinusEnergy(1);
        }

        public bool MinusEnergy(int i)
        {
            if (PMone.UnlimitedEnergy)
                return true;

            if (PMone.Energy >= i)
            {
                PMone.Energy -= i;

                Save();

                return true;
            }

            return false;
        }

        public void AddEnergy(int i)
        {
            int x = Mathf.Min(i + PMone.Energy, Portmone.MAX_ENERGY);
            PMone.Energy = x;

            Save();
        }

        public bool Byu10ForBattery()
        {
            if (MinusMoney(COST_10))
            {
                AddEnergy(10);
                return true;
            }

            return false;
        }

        public bool ByuFullForBattery()
        {
            if (MinusMoney(COST_FULL))
            {
                AddEnergy(MaxEnergy);
                return true;
            }

            return false;
        }

        public bool MinusMoney(int i)
        {
            if (PMone.Money >= i)
            {
                PMone.Money -= i;

                Save();

                return true;
            }

            return false;
        }

        public void AddMoney(int i)
        {
            PMone.Money += i;

            Save();
        }

        private void FixedUpdate()
        {
            if (PMone.DoubleEnergy && PMone.Energy < Portmone.MAX_ENERGY || !PMone.DoubleEnergy && PMone.Energy < Portmone.MAX_ENERGY)
            {
                TimeSpan span = DateTime.Now - PMone.LastRest;
                double seconds = span.TotalSeconds;
                double k = PMone.HalfRest ? 2 : 1;

                if (seconds > Portmone.REST_SECONDS / k)
                {
                    int i = (int)Math.Floor(seconds / Portmone.REST_SECONDS / k);
                    PMone.Energy += i;

                    if (PMone.DoubleEnergy && PMone.Energy > Portmone.MAX_ENERGY * 2)
                        PMone.Energy = Portmone.MAX_ENERGY * 2;
                    else if (PMone.Energy > Portmone.MAX_ENERGY)
                        PMone.Energy = Portmone.MAX_ENERGY;

                    PMone.LastRest = DateTime.Now;

                    Save();

                    //if (PMone.Energy == MaxEnergy && Application.isMobilePlatform)
                    //{
                    //    AndroidJavaObject ajc = new AndroidJavaObject("com.zeljkosassets.notifications.Notifier");
                    //    ajc.CallStatic("sendNotification", "Strom", LocalController.Instance.L("notification", "er_lable"), LocalController.Instance.L("notification", "er_text"), 5);
                    //}
                }
            }
            else
            {
                PMone.LastRest = DateTime.Now;
            }
        }

        private void Save()
        {
            Portmone.Save(PMone);

            if (OnPortmoneChanged != null)
                OnPortmoneChanged.Invoke();

            if (OnMoneyChanged != null)
                OnMoneyChanged.Invoke();

            if (OnEnergyChanged != null)
                OnEnergyChanged.Invoke();
        }
    }

    [Serializable]
    public class Portmone
    {
        public const string FILE = "/pmp.bin";

        public const int REST_SECONDS = 600;
        public const int MAX_ENERGY = 30;

        public int Energy;
        public bool DoubleEnergy = false;
        public bool HalfRest = false;
        public bool UnlimitedEnergy = false;

        public int Money;
        public bool ShowAD = true;

        public DateTime LastRest;

        #region SaveLoad

        public static Portmone Load()
        {
            if (File.Exists(Application.persistentDataPath + FILE))
            {
                try
                {
                    using (FileStream stream = new FileStream(Application.persistentDataPath + FILE, FileMode.Open))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        return (Portmone)bf.Deserialize(stream);
                    }
                }
                catch (Exception ex)
                {
                    ErrorController.Instance.Send(MarketController.Instance, ex.Message);
                    File.Delete(Application.persistentDataPath + FILE);
                    return null;
                }
            }
            return null;
        }

        public static void Save(Portmone pm)
        {
            try
            {
                using (FileStream stream = new FileStream(Application.persistentDataPath + FILE, FileMode.Create))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(stream, pm);
                }
            }
            catch (Exception ex)
            {
                ErrorController.Instance.Send(MarketController.Instance, ex.Message);
            }
        }

        #endregion SaveLoad
    }
}