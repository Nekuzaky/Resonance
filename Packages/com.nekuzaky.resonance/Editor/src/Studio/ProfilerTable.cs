using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class NameCache
    {
        #region Public

        public const int Limit = 4096;

        public string Of(Object item)
        {
            if (item == null)
                return null;
            if (_names.TryGetValue(item, out var name))
                return name;
            if (_names.Count >= Limit)
                _names.Clear();
            name = item.name;
            _names[item] = name;
            return name;
        }

        public void Clear() => _names.Clear();

        #endregion


        #region Private and Protected

        private readonly Dictionary<Object, string> _names = new();

        #endregion
    }

    internal sealed class ProfilerRow
    {
        #region Public

        public const float LevelWidth = 80f;

        public readonly VisualElement Element;

        public ProfilerRow(IReadOnlyList<float> widths, bool level)
        {
            Element = Ui.Box("rs-table-row");
            _cells = new Label[widths.Count];
            _classes = new string[widths.Count];
            _numbers = new float[widths.Count];
            for (var i = 0; i < widths.Count; i++)
            {
                _cells[i] = Ui.Styled(string.Empty, "rs-cell");
                _cells[i].style.width = widths[i];
                _numbers[i] = float.NaN;
                Element.Add(_cells[i]);
            }
            if (!level)
                return;
            var bar = Ui.Box("rs-level");
            bar.style.width = LevelWidth;
            _fill = Ui.Box("rs-level-fill");
            bar.Add(_fill);
            Element.Add(bar);
        }

        public void Set(int column, string text, string className = null)
        {
            var cell = _cells[column];
            if (cell.text != text)
                cell.text = text;
            if (_classes[column] == className)
                return;
            if (_classes[column] != null)
                cell.RemoveFromClassList(_classes[column]);
            if (className != null)
                cell.AddToClassList(className);
            _classes[column] = className;
        }

        public bool Changed(int column, float value)
        {
            if (_numbers[column].Equals(value))
                return false;
            _numbers[column] = value;
            return true;
        }

        public void SetLevel(float level)
        {
            var width = Mathf.Round(Mathf.Clamp01((AudioMath.ToDecibels(level) + 60f) / 60f) * LevelWidth);
            if (_fill == null || width.Equals(_width))
                return;
            _width = width;
            _fill.style.width = width;
        }

        #endregion


        #region Private and Protected

        private readonly Label[] _cells;
        private readonly string[] _classes;
        private readonly float[] _numbers;
        private readonly VisualElement _fill;
        private float _width = -1f;

        #endregion
    }

    internal sealed class ProfilerTable
    {
        #region Public

        public ProfilerTable(VisualElement parent, bool level, params float[] widths)
        {
            _parent = parent;
            _level = level;
            _widths = widths;
        }

        public int Count => _used;

        public void Begin() => _used = 0;

        public ProfilerRow Next()
        {
            if (_used == _rows.Count)
            {
                var created = new ProfilerRow(_widths, _level);
                _rows.Add(created);
                _parent.Add(created.Element);
            }
            var row = _rows[_used++];
            row.Element.style.display = DisplayStyle.Flex;
            return row;
        }

        public void End()
        {
            for (var i = _used; i < _rows.Count; i++)
                _rows[i].Element.style.display = DisplayStyle.None;
        }

        #endregion


        #region Private and Protected

        private readonly VisualElement _parent;
        private readonly bool _level;
        private readonly float[] _widths;
        private readonly List<ProfilerRow> _rows = new();
        private int _used;

        #endregion
    }
}
