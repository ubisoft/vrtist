using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class LogItem
    {
        public LogType type;
        public string text;
        public string stackTrace;
    }

    public sealed class FixedSizeQueue<T> : Queue<T>
    {
        public int Capacity { get; }

        public FixedSizeQueue(int capacity)
        {
            Capacity = capacity;
        }

        public new T Enqueue(T item)
        {
            base.Enqueue(item);
            if (base.Count > Capacity)
            {
                return base.Dequeue();
            }
            return default;
        }
    }

    public class ConsoleWindow : MonoBehaviour
    {
        [Tooltip("Max number of logs"), Range(10, 500)] public int capacity = 100;

        private Transform mainPanel = null;
        private UIVerticalSlider scrollbar = null;

        private UILabel[] labels = new UILabel[8];
        private bool dirty = false;
        private bool scrollDirty = false;
        private int scrollIndex = 0;

        private bool showInfos = true;
        private bool showWarnings = true;
        private bool showErrors = true;

        FixedSizeQueue<LogItem> logs;
        List<LogItem> filteredLogs = new List<LogItem>();

        int plop = 0;

        void Start()
        {
            mainPanel = transform.Find("MainPanel");
            if (mainPanel != null)
            {
                scrollbar = mainPanel.Find("Scrollbar")?.GetComponent<UIVerticalSlider>();

                for (int i = 0; i < labels.Length; ++i)
                {
                    labels[i] = mainPanel.Find($"Line_{i}").GetComponent<UILabel>();
                }
            }

            logs = new FixedSizeQueue<LogItem>(capacity);
            Application.logMessageReceived += HandleLog;

            StartCoroutine(DebugMessages());
        }

        private IEnumerator DebugMessages()
        {
            while (plop < 50)
            {
                Debug.Log($"A debug message {plop}");
                if (plop % 2 == 0) { Debug.LogWarning($"A warning {plop}"); }
                if (plop % 3 == 0) { Debug.LogError($"An error {plop}"); }
                ++plop;
                yield return null;
            }
        }

        private void Update()
        {
            if (dirty)
            {
                FilterLogs();
            }

            if (dirty || scrollDirty)
            {
                int index = 0;      // for lines
                int logsIndex = 0;  // for log items
                foreach (LogItem item in filteredLogs)
                {
                    // Apply scroll
                    if (logsIndex < scrollIndex)
                    {
                        ++logsIndex;
                        continue;
                    }

                    // Set lines
                    labels[index].Text = item.text.Substring(0, Math.Min(item.text.Length, 65));  // TMP truncate is bugged
                    if (labels[index].Text.Length < item.text.Length) { labels[index].Text += "..."; }

                    switch (item.type)
                    {
                        case LogType.Log: labels[index].Image = UIUtils.LoadIcon("info"); break;
                        case LogType.Warning: labels[index].Image = UIUtils.LoadIcon("warning"); break;
                        case LogType.Error:
                        case LogType.Exception:
                        case LogType.Assert:
                            labels[index].Image = UIUtils.LoadIcon("error");
                            break;
                    }

                    ++index;
                    if (index == labels.Length) { break; }
                }
                dirty = false;
                scrollDirty = false;
            }
        }

        private void HandleLog(string text, string stackTrace, LogType type)
        {
            logs.Enqueue(new LogItem { type = type, text = text, stackTrace = stackTrace });
            dirty = true;
        }

        public void OnScroll(int value)
        {
            scrollIndex = value;
            scrollDirty = true;
        }

        private void FilterLogs()
        {
            filteredLogs.Clear();
            foreach (LogItem item in logs)
            {
                if (!showInfos && item.type == LogType.Log) { continue; }
                if (!showWarnings && item.type == LogType.Warning) { continue; }
                if (!showErrors && (item.type == LogType.Error || item.type == LogType.Exception || item.type == LogType.Assert)) { continue; }

                filteredLogs.Add(item);
            }

            scrollbar.maxValue = Math.Max(filteredLogs.Count - 8, 0.1f);  // there are 8 lines of logs
            scrollbar.Value = scrollbar.maxValue;
            scrollIndex = (int) scrollbar.Value;
        }

        private void ClearLabels()
        {
            foreach (UILabel label in labels)
            {
                label.Text = "";
                label.Image = UIUtils.LoadIcon("empty");
            }
        }

        public void Clear()
        {
            logs.Clear();
            filteredLogs.Clear();
            scrollIndex = 0;
            scrollbar.maxValue = 0.1f;
            ClearLabels();
        }

        public void ShowErrors(bool value)
        {
            showErrors = value;
            ClearLabels();
            dirty = true;
        }

        public void ShowWarnings(bool value)
        {
            showWarnings = value;
            ClearLabels();
            dirty = true;
        }

        public void ShowInfos(bool value)
        {
            showInfos = value;
            ClearLabels();
            dirty = true;
        }
    }
}
