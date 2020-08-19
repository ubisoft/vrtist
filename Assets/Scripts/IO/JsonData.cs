using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VRtist
{
    // WARNING: Unity and its version of C# is not able to deserialize a Dictionary<string, object>.
    // System.Text.Json or Newtonsoft.Json don't work :( in this case. So the following classes are
    // not used. We keep them for an upgrade someday. So right now we use the JsonHelper class below.
    //
    // Classes to read following Jsons:
    // {
    // "127.0.0.1:3380": {
    //     "id": "127.0.0.1:3380",
    //     "ip": "127.0.0.1",
    //     "port": 3380,
    //     "room": null
    // }
    // }
    //
    // {"127.0.0.1:3380": {"room": "Local"}}
    //
    // {"127.0.0.1:3380": {"user_name": "VRtist"}}
    //
    // {
    // "127.0.0.1:59951": {
    //     "user_scenes": {
    //         "Scene": {
    //             "frame": 67,
    //             "selected_objects": [],
    //             "views": {
    //                 "1662222059768": {
    //                     "eye": [21.046432495117188, -5.221196174621582, 5.855473041534424],
    //                     "target": [1.233099102973938, 0.9093819260597229, -0.12044590711593628],
    //                     "screen_corners": [
    //                         [20.247392654418945, -5.561498641967773, 5.3597612380981445],
    //                         [20.58077621459961, -4.4979352951049805, 5.345513820648193],
    //                         [20.424291610717773, -4.441169261932373, 5.922540664672852],
    //                         [20.090919494628906, -5.504717826843262, 5.936784267425537]
    //                     ]
    //                 }
    //             }
    //         }
    //     }
    // }
    // }
    public class JsonClientId
    {
        public string id { get; set; }
        public string ip { get; set; }
        public int port { get; set; }
        public string room { get; set; }
        public string user_name { get; set; }
        public JsonUserScenes user_scenes { get; set; }
    }

    public class JsonUserScenes
    {
        public JsonScene Scene { get; set; }
    }

    public class JsonScene
    {
        public int frame { get; set; }
        public List<int> selected_objects { get; set; }
        public Dictionary<string, JsonView> views { get; set; }
    }

    public class JsonView
    {
        public List<float> eye { get; set; }
        public List<float> target { get; set; }
    }

    // End of Json deserializer classes


    // A value read from a json. May exist, be valid or not.
    public class JsonValue<T>
    {
        public bool exist;
        public bool valid;
        public T value;
        public bool IsValid { get { return exist && valid; } }
    }

    // Flat client info
    public class ClientInfo
    {
        public JsonValue<string> id;
        public JsonValue<string> ip;
        public JsonValue<int> port;
        public JsonValue<string> room;
        public JsonValue<string> userName;
        public JsonValue<Color> userColor;
        public JsonValue<string> viewId;
        public JsonValue<Vector3> eye;
        public JsonValue<Vector3> target;
    }

    public static class JsonHelper
    {
        // {
        // "127.0.0.1:12639": {
        //     "user_name": "sylvain",
        //     "user_color": [0.8018945455551147, 0.21085186302661896, 0.9761602282524109],
        //     "blender_windows": [{
        //         "scene": "Scene",
        //         "view_layer": "View Layer",
        //         "screen": "Layout",
        //         "areas_3d": ["1129407154056"]
        //     }],
        //     "user_scenes": {
        //         "Scene": {
        //             "frame": 1,
        //             "selected_objects": ["Cube"],
        //             "views": {
        //                 "1129407154056": {
        //                     "eye": [14.727903366088867, -6.505107879638672, 8.018034934997559],
        //                     "target": [-0.0, -0.0, -0.0],
        //                     "screen_corners": [
        //                         [14.005969047546387, -6.794801712036133, 7.3896331787109375],
        //                         [14.458544731140137, -5.788208484649658, 7.374994277954102],
        //                         [14.194585800170898, -5.660981178283691, 7.963055610656738],
        //                         [13.742013931274414, -6.66757345199585, 7.977694034576416]
        //                     ]
        //                 }
        //             }
        //         }
        //     },
        //     "id": "127.0.0.1:12639",
        //     "ip": "127.0.0.1",
        //     "port": 12639,
        //     "room": "Local"
        // },
        // "127.0.0.1:12644": {
        //     "user_name": "VRtist",
        //     "id": "127.0.0.1:12644",
        //     "ip": "127.0.0.1",
        //     "port": 12644,
        //     "room": "Local"
        // }
        // }
        private static List<string> ExtractClients(string json)
        {
            List<string> clients = new List<string>();
            int count = 0;
            int index = 0;
            int start = -1;
            int end = -1;

            foreach (char c in json)
            {
                if ('{' == c) { ++count; }
                if ('}' == c) { --count; }

                if (1 == count && -1 == start && '"' == c) { start = index; }
                else if (1 == count && -1 == end && '}' == c)
                {
                    end = index;
                    clients.Add(json.Substring(start, end - start));
                    start = end = -1;
                }

                ++index;
            }

            return clients;
        }

        private static Regex idRegex = new Regex("^{?\"(?<value>.+?)\"", RegexOptions.Compiled);
        private static Regex ipRegex = new Regex("\"ip\":\\s*\"(?<value>.+?)\"", RegexOptions.Compiled);
        private static Regex portRegex = new Regex("\"port\":\\s*(?<value>\\d+)", RegexOptions.Compiled);
        private static Regex roomRegex = new Regex("\"room\":\\s*(\"(?<value>(.+?))\")|(?<value>null)", RegexOptions.Compiled);
        private static Regex userNameRegex = new Regex("\"user_name\":\\s*\"(?<value>.+?)\"", RegexOptions.Compiled);
        private static Regex viewRegex = new Regex("\"views\":\\s*{\\s*\"(?<value>.+?)\"", RegexOptions.Compiled);
        private static Regex eyeRegex = new Regex("\"eye\":\\s*\\[(?<x>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?[0-9]+)?),\\s(?<y>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?[0-9]+)?),\\s(?<z>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?[0-9]+)?)]", RegexOptions.Compiled);
        private static Regex targetRegex = new Regex("\"target\":\\s*\\[(?<x>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?[0-9]+)?),\\s(?<y>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?[0-9]+)?),\\s(?<z>[-+]?([0-9]*[.])?[0-9]+([eE][-+]?[0-9]+)?)]", RegexOptions.Compiled);
        private static Regex userColorRegex = new Regex("\"user_color\":\\s*\\[(?<r>[-+]?([0-9]*[.])?[0-9]+),\\s(?<g>[-+]?([0-9]*[.])?[0-9]+),\\s(?<b>[-+]?([0-9]*[.])?[0-9]+)(,\\s(?<a>[-+]?([0-9]*[.])?[0-9]+))?]", RegexOptions.Compiled);

        private static JsonValue<string> ExtractStringInfo(string json, Regex regex)
        {
            JsonValue<string> jsonValue = new JsonValue<string>();
            Match match = regex.Match(json);
            if (match.Success)
            {
                jsonValue.exist = true;
                jsonValue.valid = true;
                jsonValue.value = match.Groups["value"].Value;
            }
            return jsonValue;
        }

        private static JsonValue<int> ExtractIntInfo(string json, Regex regex)
        {
            JsonValue<int> jsonValue = new JsonValue<int>();
            Match match = regex.Match(json);
            if (match.Success)
            {
                jsonValue.exist = true;
                jsonValue.valid = true;
                if (int.TryParse(match.Groups["value"].Value, out int value))
                {
                    jsonValue.value = value;
                }
                else { jsonValue.valid = false; }
            }
            return jsonValue;
        }

        private static JsonValue<Vector3> ExtractVector3Info(string json, Regex regex)
        {
            JsonValue<Vector3> jsonValue = new JsonValue<Vector3>();
            Match match = regex.Match(json);
            if (match.Success)
            {
                jsonValue.exist = true;
                jsonValue.valid = true;
                try
                {
                    float x = float.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture.NumberFormat);
                    float y = float.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture.NumberFormat);
                    float z = float.Parse(match.Groups["z"].Value, CultureInfo.InvariantCulture.NumberFormat);
                    jsonValue.value = new Vector3(x, y, z);
                }
                catch (Exception)
                {
                    jsonValue.valid = false;
                }
            }
            return jsonValue;
        }

        private static JsonValue<Color> ExtractColorInfo(string json, Regex regex)
        {
            JsonValue<Color> jsonValue = new JsonValue<Color>();
            Match match = regex.Match(json);
            if (match.Success)
            {
                jsonValue.exist = true;
                jsonValue.valid = true;
                try
                {
                    float r = float.Parse(match.Groups["r"].Value, CultureInfo.InvariantCulture.NumberFormat);
                    float g = float.Parse(match.Groups["g"].Value, CultureInfo.InvariantCulture.NumberFormat);
                    float b = float.Parse(match.Groups["b"].Value, CultureInfo.InvariantCulture.NumberFormat);
                    float a;
                    try
                    {
                        a = float.Parse(match.Groups["a"].Value, CultureInfo.InvariantCulture.NumberFormat);
                    }
                    catch (Exception)
                    {
                        a = 1f;
                    }
                    jsonValue.value = new Color(r, g, b, a);
                }
                catch (Exception)
                {
                    jsonValue.valid = false;
                }
            }
            return jsonValue;
        }

        public static ClientInfo GetClientInfo(string json)
        {
            ClientInfo clientInfo = new ClientInfo();
            clientInfo.id = ExtractStringInfo(json, idRegex);
            clientInfo.ip = ExtractStringInfo(json, ipRegex);
            clientInfo.port = ExtractIntInfo(json, portRegex);
            clientInfo.room = ExtractStringInfo(json, roomRegex);
            clientInfo.userName = ExtractStringInfo(json, userNameRegex);
            clientInfo.userColor = ExtractColorInfo(json, userColorRegex);
            clientInfo.viewId = ExtractStringInfo(json, viewRegex);
            clientInfo.eye = ExtractVector3Info(json, eyeRegex);
            clientInfo.target = ExtractVector3Info(json, targetRegex);
            return clientInfo;
        }

        public static List<ClientInfo> GetClientsInfo(string json)
        {
            List<ClientInfo> clientsInfo = new List<ClientInfo>();
            List<string> jsonClients = ExtractClients(json);
            foreach (string jsonClient in jsonClients)
            {
                clientsInfo.Add(GetClientInfo(jsonClient));
            }
            return clientsInfo;
        }

        // {
        //     "user_scenes": {
        //         "Scene": {
        //             "frame": 67,
        //             "selected_objects": [],
        //             "views": {
        //                 "1662222059768": {
        //                     "eye": [21.046432495117188, -5.221196174621582, 5.855473041534424],
        //                     "target": [1.233099102973938, 0.9093819260597229, -0.12044590711593628],
        //                     "screen_corners": [
        //                         [20.247392654418945, -5.561498641967773, 5.3597612380981445],
        //                         [20.58077621459961, -4.4979352951049805, 5.345513820648193],
        //                         [20.424291610717773, -4.441169261932373, 5.922540664672852],
        //                         [20.090919494628906, -5.504717826843262, 5.936784267425537]
        //                     ]
        //                 }
        //             }
        //         }
        //     }
        // }
        public static string CreateJsonPlayerInfo(ConnectedUser user)
        {
            if (null == user.id || null == user.viewId) { return null; }
            string json = "{\"user_scenes\": {" +
                "\"Scene\": {" +
                "\"frame\": 1," +
                "\"selected_objects\": []," +
                "\"views\": {" +
                $"\"{user.viewId}\": {{" +
                $"\"eye\": [{user.eye.ToString().Substring(1, user.eye.ToString().Length - 2)}]," +
                $"\"target\": [{user.target.ToString().Substring(1, user.target.ToString().Length - 2)}]," +
                "\"screen_corners\": [" +
                $"[{user.corners[0].ToString().Substring(1, user.corners[0].ToString().Length - 2)}]" +
                $", [{user.corners[1].ToString().Substring(1, user.corners[1].ToString().Length - 2)}]" +
                $", [{user.corners[2].ToString().Substring(1, user.corners[2].ToString().Length - 2)}]" +
                $", [{user.corners[3].ToString().Substring(1, user.corners[3].ToString().Length - 2)}]]" +
                "}}}}}";
            return json;
        }
    }
}
