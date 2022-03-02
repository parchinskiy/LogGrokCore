﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Controls.TextRender;

public abstract class Outline
{
    public static Outline None => new None();
}

public sealed class None : Outline
{
}

public abstract class Expandable : Outline
{
    private readonly int _index;
    private readonly Action<int> _toggle;
 
    protected Expandable(int index, Action<int> toggle)
    {
        _index = index;
        _toggle = toggle;
    }

    public void Toggle()
    {
        _toggle(_index);
    }   
}

public sealed class ExpandedUpper : Expandable
{
    public ExpandedUpper(int index, Action<int> toggle) 
        : base(index, toggle)
    {
    }
}
public sealed class ExpandedLower : Expandable
{
    public ExpandedLower(int index, Action<int> toggle) 
        : base(index, toggle)
    {
    }
}

public sealed class Collapsed : Expandable
{
    public Collapsed(int index, Action<int> toggle) : base(index, toggle)
    {
    }
}

public class CollapsibleRegionsMachine
{
    public (Outline, int) this[int index] => _regions[index];
    public int LineCount => _regions.Count;

    private readonly List<(Outline, int)> _regions;
    private readonly (bool isCollapsed, int start, int length)[] _collapsibleRegions;
    private readonly int[] _lines;
    private readonly Action<int> _toggleAction;

    public CollapsibleRegionsMachine(int lineCount, (int start, int length)[] collapsibleRegions)
    {
        _lines = Enumerable.Range(0, lineCount).ToArray();
        _collapsibleRegions = collapsibleRegions.Select(static region 
            => (false, region.start, region.length)).ToArray();
        _regions = new List<(Outline, int)>(collapsibleRegions.Length);
        _toggleAction = Toggle;
        Update();
    }

    public event Action? Changed;
    
    private void Update()
    {
        _regions.Clear();
        for (var i = 0; i < _lines.Length; i++)
        {
            var rangeStart=
                _collapsibleRegions.FirstOrDefault(r => r.start == i);
            
            var rangeEnd = 
                _collapsibleRegions.FirstOrDefault(r => r.start + r.length == i);
            
            var outline = (rangeStart, rangeEnd) switch
            {
                ((false,0,0), (false,0,0)) => Outline.None,
                ((false, _, _), (false, 0,0)) => new ExpandedUpper(i, _toggleAction),
                ((false,0,0), (false, _, _)) => new ExpandedLower(i, _toggleAction),
                ((true, _, _), (false, 0, 0)) => new Collapsed(i, _toggleAction),
                _ => throw new InvalidOperationException()
            };
            
            _regions.Add((outline, _lines[i]));

            if (outline is Collapsed)
            {
                i += rangeStart.length;
            }
        }
        
        Changed?.Invoke();
    }

    private void Toggle(int index)
    {
        for (var i = 0; i < _collapsibleRegions.Length; i++)
        {
            var (isCollapsed, start, length) = _collapsibleRegions[i];
            if (start != index && start + length != index) continue;
            _collapsibleRegions[i] = (!isCollapsed, start, length);
            Update();
            return;
        } 
    }
}