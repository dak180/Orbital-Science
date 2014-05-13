﻿/******************************************************************************
                   Science Alert for Kerbal Space Program                    
 ******************************************************************************
    Copyright (C) 2014 Allen Mrazek (amrazek@hotmail.com)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *****************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DMagic
{

    /// <summary>
    /// When the user decides to transmit data, the game selects "the best"
    /// antenna to use and moves the ScienceData out of its experiment or
    /// container into the transmitter.  This poses us two problems:
    ///   1) There's no way for us to get at the data to be transmitted, so
    ///      we don't know which info is being transmitted (or when it was, no event)
    ///   2) Since the data was taken out of storage, we temporarily lose
    ///      track of it until the transmitter has finished submitting it
    ///      
    /// As a result, while we're examining a given experiment to see if 
    /// it should trigger a response we might find no info in R&D and no
    /// info in storage resulting in the plugin erroneously concluding that
    /// the experiment is fresh in the interim time while the results are
    /// being transmitted by an antenna.
    /// 
    /// This method involves tricking the game into thinking there's a fake
    /// transmitter on the vessel. Whenever it goes looking for a transmitter
    /// to use, our magic version will score highest and be chosen. It will
    /// receive the data to be transmitted and delegate that data to real
    /// transmitters itself.
    /// 
    /// There are a couple of edge cases to be solved still, however. For
    /// instance, a player can manually select a transmitter, click "transmit"
    /// and bypass this transmitter. I haven't fixed this yet because I still
    /// need to determine whether anyone actually uses this method of transmission.
    /// 
    /// Note: Why not use MM to edit the transmitter module itself with our own
    /// version? It will contaminate the player's save. Also I'm experimenting 
    /// with adding PartModules on-the-fly
    /// </summary>
    using ScienceDataList = List<ScienceData>;

    public class MagicDataTransmitter : PartModule, IScienceDataTransmitter
    {
        // keep track of which (REAL) transmitter is transmitting what
        private Dictionary<IScienceDataTransmitter, ScienceDataList> realTransmitters = new Dictionary<IScienceDataTransmitter, ScienceDataList>();

        // If all transmitters are busy, we'll need a mechanism to wait for one
        // to become ready. It seems that sending data to a busy transmitter
        // will result in it getting stalled, remaining open but
        // not sending data
        private Dictionary<IScienceDataTransmitter, ScienceDataList> toBeTransmitted = new Dictionary<IScienceDataTransmitter, ScienceDataList>();


        /// <summary>
        /// Called just as the PartModule is added to a Part.  We can
        /// count on doing our initialization here because ScienceAlert
        /// will destroy older copies of MagicDataTransmitters by default.  This
        /// is done mainly so we don't have to keep track of the MagicDataTransmitter
        /// instance and can restrict any updates to here.
        /// </summary>
        public void Start()
        {
            print("MagicDataTransmitter started");

            // locate all available real transmitters
            var transmitters = FlightGlobals.ActiveVessel.FindPartModulesImplementing<IScienceDataTransmitter>();

            // remove the fake ones ..
            transmitters.RemoveAll(tx => tx is MagicDataTransmitter);

            foreach (var tx in transmitters)
            {
                realTransmitters.Add(tx, new ScienceDataList());
                toBeTransmitted.Add(tx, new ScienceDataList());
            }

            print(String.Format("MagicDataTransmitter has found {0} useable transmitters", transmitters.Count));
        }



        public void Update()
        {
            // keep an eye on queued transmission data and send it as
            // as soon as its assigned transmitter becomes available
            var txs = toBeTransmitted.Keys;

            foreach (var tx in txs)
                if (toBeTransmitted[tx].Count > 0)
                    if (!tx.IsBusy() && tx.CanTransmit())
                    {
                        realTransmitters[tx].AddRange(toBeTransmitted[tx]);
                        toBeTransmitted[tx].Clear();
                        tx.TransmitData(realTransmitters[tx]);
                    }
        }



        public override void OnSave(ConfigNode node)
        {
            node.ClearData(); // don't save anything about MagicDataTransmitter or
                              // the save file will be poisoned
        }



        public override void OnLoad(ConfigNode node)
        {
            // empty
        }


        /// <summary>
        /// A suitable transmitter has been selected to submit some data. We'll queue
        /// it for transmission in the next Update().
        /// </summary>
        /// <param name="data"></param>
        /// <param name="transmitter"></param>
        private void QueueTransmission(ScienceDataList data, IScienceDataTransmitter transmitter)
        {
            if (data.Count == 0)
                return;

#if DEBUG
            if (!realTransmitters.ContainsKey(transmitter))
                print("MagicDataTransmitter.DoTransmit - Given transmitter isn't in real transmitter list!");
#endif

            toBeTransmitted[transmitter].AddRange(data);
        }



        /// <summary>
        /// Locate a suitable transmitter and queue this data up for it
        /// </summary>
        /// <param name="data"></param>
        void IScienceDataTransmitter.TransmitData(ScienceDataList data)
        {
            print(String.Format("MagicTransmitter: received {0} ScienceData entries", data.Count));

            // locate the best actual transmitter to send this data through
            // lower scores seem to be better
            var potentials = new List<IScienceDataTransmitter>();

            foreach (var kvp in realTransmitters)
                potentials.Add(kvp.Key);

            if (potentials.Count > 0)
            {
                potentials = potentials.OrderBy(potential => ScienceUtil.GetTransmitterScore(potential)).ToList();

                QueueTransmission(data, potentials.First());
            }
            else print("MagicDataTransmitter: Did not find any real transmitters");
        }

        bool IScienceDataTransmitter.IsBusy()
        {
            return false;  // magic transmitter is always ready
        }

        bool IScienceDataTransmitter.CanTransmit()
        {
            return realTransmitters.Any(pair => pair.Key.CanTransmit());
        }

        float IScienceDataTransmitter.DataRate
        {
            get
            {
                return float.MaxValue;
            }
        }

        double IScienceDataTransmitter.DataResourceCost
        {
            get
            {
                return 0d;
            }
        } 

        
        public ScienceDataList     QueuedData
        {
            get
            {
                ScienceDataList list = new ScienceDataList();

                foreach (var kvp in realTransmitters)
                {
                    if (!kvp.Key.IsBusy())
                        kvp.Value.Clear(); // it's not doing anything, therefore nothing is queued

                    list.AddRange(kvp.Value);
                    list.AddRange(toBeTransmitted[kvp.Key]);
                }

                return list;
            }
        }

        public override string ToString()
        {
            return string.Format("MagicDataTransmitter attached to {0}; {1} entries in queue", FlightGlobals.ActiveVessel.rootPart.ConstructID, QueuedData.Count);
        }
    }
}