using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/ControllerPath")]
    public class ControllerPath : ScriptableObject
    {

        public string RightQuestController;
        public string LeftQuestController;
        public string RightQuest2Controller;
        public string LeftQuest2Controller;
        public string RightIndexController;
        public string LeftIndexController;

        public string QuestRoot;
        public string Quest2Root;
        public string IndexRoot;


        public string GetControllerPath(bool isRight, VRControllerManager.ControllerModel model)
        {
            switch (model)
            {
                case VRControllerManager.ControllerModel.Quest: return isRight ? RightQuestController : LeftQuestController;
                case VRControllerManager.ControllerModel.Quest2: return isRight ? RightQuest2Controller : LeftQuest2Controller;
                case VRControllerManager.ControllerModel.Index: return isRight ? RightIndexController : LeftIndexController;
                default: return isRight ? RightQuestController : LeftQuestController;
            }
        }

        public string GetControllerRoot(VRControllerManager.ControllerModel model)
        {
            switch (model)
            {
                case VRControllerManager.ControllerModel.Quest: return QuestRoot;
                case VRControllerManager.ControllerModel.Quest2: return Quest2Root;
                case VRControllerManager.ControllerModel.Index: return IndexRoot;
                default: return QuestRoot;
            }
        }
    }

}