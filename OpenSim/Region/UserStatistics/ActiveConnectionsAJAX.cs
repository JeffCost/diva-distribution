﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Mono.Data.SqliteClient;
using OpenMetaverse;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Framework.Statistics;

namespace OpenSim.Region.UserStatistics
{
    public class ActiveConnectionsAJAX : IStatsController
    {
        #region IStatsController Members

        public Hashtable ProcessModel(Hashtable pParams)
        {
            
            List<Scene> m_scene = (List<Scene>)pParams["Scenes"];

            Hashtable nh = new Hashtable();
            nh.Add("hdata", m_scene);

            return nh;
        }

        public string RenderView(Hashtable pModelResult)
        {
            List<Scene> all_scenes = (List<Scene>) pModelResult["hdata"];

            StringBuilder output = new StringBuilder();
            HTMLUtil.OL_O(ref output, "");
            foreach (Scene scene in all_scenes)
            {
                List<ScenePresence> avatarInScene = scene.GetScenePresences();

                HTMLUtil.LI_O(ref output, "");
                output.Append(scene.RegionInfo.RegionName);
                HTMLUtil.OL_O(ref output, "");
                foreach (ScenePresence av in avatarInScene)
                {
                    Dictionary<string,string> queues = new Dictionary<string, string>();
                    if (av.ControllingClient is IStatsCollector)
                    {
                        IStatsCollector isClient = (IStatsCollector) av.ControllingClient;
                        queues = decodeQueueReport(isClient.Report());
                    }
                    HTMLUtil.LI_O(ref output, "");
                    output.Append(av.Name);
                    output.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
                    output.Append((av.IsChildAgent ? "Child" : "Root"));
                    
                    Dictionary<string, int> throttles = DecodeClientThrottles(av.ControllingClient.GetThrottlesPacked(1));
                    
                    HTMLUtil.UL_O(ref output, "");
                    foreach (string throttlename in throttles.Keys)
                    {
                        HTMLUtil.LI_O(ref output, "");
                        output.Append(throttlename);
                        output.Append(":");
                        output.Append(throttles[throttlename].ToString());
                        if (queues.ContainsKey(throttlename))
                        {
                            output.Append("/");
                            output.Append(queues[throttlename]);
                        }
                        HTMLUtil.LI_C(ref output);
                    }
                    if (queues.ContainsKey("Incoming") && queues.ContainsKey("Outgoing"))
                    {
                        HTMLUtil.LI_O(ref output, "red");
                        output.Append("SEND:");
                        output.Append(queues["Outgoing"]);
                        output.Append("/");
                        output.Append(queues["Incoming"]);
                        HTMLUtil.LI_C(ref output);
                    }

                    HTMLUtil.UL_C(ref output);
                    HTMLUtil.LI_C(ref output);
                }
                HTMLUtil.OL_C(ref output);
            }
            HTMLUtil.OL_C(ref output);
            return output.ToString();
        }

        public Dictionary<string, int> DecodeClientThrottles(byte[] throttle)
        {
            Dictionary<string, int> returndict = new Dictionary<string, int>();
            // From mantis http://opensimulator.org/mantis/view.php?id=1374
            // it appears that sometimes we are receiving empty throttle byte arrays.
            // TODO: Investigate this behaviour
            if (throttle.Length == 0)
            {
                return new Dictionary<string, int>();
            }

            int tResend = -1;
            int tLand = -1;
            int tWind = -1;
            int tCloud = -1;
            int tTask = -1;
            int tTexture = -1;
            int tAsset = -1;
            int tall = -1;
            const int singlefloat = 4;

            //Agent Throttle Block contains 7 single floatingpoint values.
            int j = 0;

            // Some Systems may be big endian...
            // it might be smart to do this check more often...
            if (!BitConverter.IsLittleEndian)
                for (int i = 0; i < 7; i++)
                    Array.Reverse(throttle, j + i * singlefloat, singlefloat);

            // values gotten from OpenMetaverse.org/wiki/Throttle.  Thanks MW_
            // bytes
            // Convert to integer, since..   the full fp space isn't used.
            tResend = (int)BitConverter.ToSingle(throttle, j);
            returndict.Add("Resend", tResend);
            j += singlefloat;
            tLand = (int)BitConverter.ToSingle(throttle, j);
            returndict.Add("Land", tLand);
            j += singlefloat;
            tWind = (int)BitConverter.ToSingle(throttle, j);
            returndict.Add("Wind", tWind);
            j += singlefloat;
            tCloud = (int)BitConverter.ToSingle(throttle, j);
            returndict.Add("Cloud", tCloud);
            j += singlefloat;
            tTask = (int)BitConverter.ToSingle(throttle, j);
            returndict.Add("Task", tTask);
            j += singlefloat;
            tTexture = (int)BitConverter.ToSingle(throttle, j);
            returndict.Add("Texture", tTexture);
            j += singlefloat;
            tAsset = (int)BitConverter.ToSingle(throttle, j);
            returndict.Add("Asset", tAsset);

            tall = tResend + tLand + tWind + tCloud + tTask + tTexture + tAsset;
            returndict.Add("All", tall);

            return returndict;
        }
        public Dictionary<string,string> decodeQueueReport(string rep)
        {
            Dictionary<string, string> returndic = new Dictionary<string, string>();
            if (rep.Length == 79)
            {
                int pos = 1;
                returndic.Add("All", rep.Substring((6 * pos), 8)); pos++;
                returndic.Add("Incoming", rep.Substring((7 * pos), 8)); pos++;
                returndic.Add("Outgoing", rep.Substring((7 * pos) , 8)); pos++;
                returndic.Add("Resend", rep.Substring((7 * pos) , 8)); pos++;
                returndic.Add("Land", rep.Substring((7 * pos) , 8)); pos++;
                returndic.Add("Wind", rep.Substring((7 * pos) , 8)); pos++;
                returndic.Add("Cloud", rep.Substring((7 * pos) , 8)); pos++;
                returndic.Add("Task", rep.Substring((7 * pos) , 8)); pos++;
                returndic.Add("Texture", rep.Substring((7 * pos), 8)); pos++;
                returndic.Add("Asset", rep.Substring((7 * pos), 8)); 
                /*
                 * return string.Format("{0,7} {1,7} {2,7} {3,7} {4,7} {5,7} {6,7} {7,7} {8,7} {9,7}",
                                 SendQueue.Count(),
                                 IncomingPacketQueue.Count,
                                 OutgoingPacketQueue.Count,
                                 ResendOutgoingPacketQueue.Count,
                                 LandOutgoingPacketQueue.Count,
                                 WindOutgoingPacketQueue.Count,
                                 CloudOutgoingPacketQueue.Count,
                                 TaskOutgoingPacketQueue.Count,
                                 TextureOutgoingPacketQueue.Count,
                                 AssetOutgoingPacketQueue.Count);
                 */
            }



            return returndic;
        }
        #endregion
    }
}