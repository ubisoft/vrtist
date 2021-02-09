
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class ProjectItem : ListItemContent
    {
        [HideInInspector] public UIDynamicListItem item;

        //public UIButton copyButton = null;
        //public UIButton deleteButton = null;

        public void OnDestroy()
        {
            //gradientPreview.onClickEvent.RemoveAllListeners();
            //copyButton.onClickEvent.RemoveAllListeners();
            //copyButton.onReleaseEvent.RemoveAllListeners();
            //deleteButton.onClickEvent.RemoveAllListeners();
            //deleteButton.onReleaseEvent.RemoveAllListeners();
        }

        public override void SetSelected(bool value)
        {
            //gradientPreview.Selected = value;
            //copyButton.Selected = value;
            //copyButton.Selected = value;
            //deleteButton.Selected = value;
        }

        public void SetListItem(UIDynamicListItem dlItem, string path)
        {
            item = dlItem;
            Texture2D texture = Utils.LoadTexture(path, true);
            transform.Find("Content").gameObject.GetComponent<MeshRenderer>().material.SetTexture("_EquiRect", texture);

            string projectName = Directory.GetParent(path).Name;
            transform.Find("Canvas/Text").gameObject.GetComponent<TextMeshProUGUI>().text = projectName;

            //gradientPreview.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            //copyButton.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            //deleteButton.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
        }

        public void AddListeners(UnityAction duplicateAction, UnityAction deleteAction)
        {
            //copyButton.onReleaseEvent.AddListener(duplicateAction);
            //deleteButton.onReleaseEvent.AddListener(deleteAction);
        }

        //public static ProjectItem GenerateItem(SkySettings sky)
        //{
        //    GameObject root = new GameObject("ProjectItem");
        //    ProjectItem gradientItem = root.AddComponent<ProjectItem>();
        //    root.layer = LayerMask.NameToLayer("CameraHidden");

        //    // Set the item invisible in order to hide it while it is not added into
        //    // a list. We will activate it after it is added
        //    root.transform.localScale = Vector3.zero;

        //    //
        //    // Background Panel
        //    //
        //    UIPanel panel = UIPanel.Create(new UIPanel.CreatePanelParams
        //    {
        //        parent = root.transform,
        //        widgetName = "GradientPreviewBackgroundPanel",
        //        relativeLocation = new Vector3(0.01f, -0.01f, -UIPanel.default_element_thickness),
        //        width = 0.145f,
        //        height = 0.185f,
        //        margin = 0.005f
        //    });
        //    panel.SetLightLayer(3);

        //    //
        //    // Gradient Button
        //    //
        //    UIGradientPreview gradientPreview = UIGradientPreview.Create(new UIGradientPreview.CreateParams
        //    {
        //        parent = panel.transform,
        //        widgetName = "GradientPreview",
        //        relativeLocation = new Vector3(0.0725f, -0.0725f, -UIGradientPreview.default_thickness),
        //        width = 0.12f,
        //        height = 0.12f,
        //        margin = 0.001f
        //    });
        //    gradientPreview.SetLightLayer(3);
        //    gradientPreview.Colors = sky;
        //    gradientPreview.NeedsRebuild = true;

        //    //
        //    // Copy Button
        //    //
        //    UIButton copyButton = UIButton.Create(new UIButton.CreateButtonParams
        //    {
        //        parent = panel.transform,
        //        widgetName = "CopyButton",
        //        relativeLocation = new Vector3(0.075f, -0.15f, -UIButton.default_thickness),
        //        width = 0.03f,
        //        height = 0.03f,
        //        icon = UIUtils.LoadIcon("duplicate"),
        //        buttonContent = UIButton.ButtonContent.ImageOnly,
        //        margin = 0.001f,
        //    });
        //    copyButton.SetLightLayer(3);

        //    //
        //    // Delete Button
        //    //
        //    UIButton deleteButton = UIButton.Create(new UIButton.CreateButtonParams
        //    {
        //        parent = panel.transform,
        //        widgetName = "DeleteButton",
        //        relativeLocation = new Vector3(0.11f, -0.15f, -UIButton.default_thickness),
        //        width = 0.03f,
        //        height = 0.03f,
        //        icon = UIUtils.LoadIcon("trash"),
        //        buttonContent = UIButton.ButtonContent.ImageOnly,
        //        margin = 0.001f,
        //    });
        //    deleteButton.SetLightLayer(3);

        //    gradientItem.gradientPreview = gradientPreview;
        //    gradientItem.copyButton = copyButton;
        //    gradientItem.deleteButton = deleteButton;
        //    gradientItem.backgroundPanel = panel;

        //    return gradientItem;
        //}
    }
}
