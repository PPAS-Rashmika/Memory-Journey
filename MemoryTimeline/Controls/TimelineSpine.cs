using System;
using System.Windows;
using System.Windows.Media;

namespace MemoryTimeline.Controls;

/// <summary>
/// Renders the central serpentine "Memory Line" spine segment with Bezier curves,
/// glowing nodes, and horizontal branch connectors to alternating left/right cards.
/// </summary>
public class TimelineSpine : FrameworkElement
{
    public static readonly DependencyProperty IsLeftProperty =
        DependencyProperty.Register(nameof(IsLeft), typeof(bool), typeof(TimelineSpine),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsFirstProperty =
        DependencyProperty.Register(nameof(IsFirst), typeof(bool), typeof(TimelineSpine),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsLastProperty =
        DependencyProperty.Register(nameof(IsLast), typeof(bool), typeof(TimelineSpine),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsMilestoneProperty =
        DependencyProperty.Register(nameof(IsMilestone), typeof(bool), typeof(TimelineSpine),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsLeft
    {
        get => (bool)GetValue(IsLeftProperty);
        set => SetValue(IsLeftProperty, value);
    }

    public bool IsFirst
    {
        get => (bool)GetValue(IsFirstProperty);
        set => SetValue(IsFirstProperty, value);
    }

    public bool IsLast
    {
        get => (bool)GetValue(IsLastProperty);
        set => SetValue(IsLastProperty, value);
    }

    public bool IsMilestone
    {
        get => (bool)GetValue(IsMilestoneProperty);
        set => SetValue(IsMilestoneProperty, value);
    }

    public TimelineSpine()
    {
        ClipToBounds = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        double midX = w / 2.0;

        // Position of the node vertically: aligns gracefully with card top media strip
        double nodeY = Math.Max(40.0, Math.Min(h * 0.35, 75.0));

        // Lateral swing: Left curve peaks at X=26, Right curve peaks at X=w-26
        double nodeX = IsLeft ? 26.0 : (w - 26.0);
        double cardEdgeX = IsLeft ? 0.0 : w;

        // Colors
        Color accentColor = Color.FromRgb(233, 69, 96); // Vibrant crimson from theme
        if (Application.Current?.TryFindResource("AccentBrush") is SolidColorBrush ab)
        {
            accentColor = ab.Color;
        }

        Color glowColor = Color.FromArgb(50, accentColor.R, accentColor.G, accentColor.B);

        var glowPen = new Pen(new SolidColorBrush(glowColor), 8.0)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        var corePen = new Pen(new SolidColorBrush(accentColor), 3.5)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        glowPen.Freeze();
        corePen.Freeze();

        // 1. Horizontal Branch Connector from Node to Card Edge
        var branchPen = new Pen(new SolidColorBrush(accentColor), 2.5)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        branchPen.Freeze();
        dc.DrawLine(branchPen, new Point(nodeX, nodeY), new Point(cardEdgeX, nodeY));

        // Connection bead where the branch meets the card
        var beadBrush = new SolidColorBrush(accentColor);
        beadBrush.Freeze();
        dc.DrawEllipse(beadBrush, null, new Point(cardEdgeX, nodeY), 3.0, 3.0);

        // 2. Serpentine Winding Bezier Curve
        double startY = IsFirst ? 14.0 : 0.0;
        Point pStart = new Point(midX, startY);
        Point pNode = new Point(nodeX, nodeY);

        double endY = IsLast ? (nodeY + 30.0) : h;
        Point pEnd = new Point(midX, endY);

        var pathGeometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = pStart, IsFilled = false };

        // Curve segment 1: from pStart (midX, startY) to pNode (nodeX, nodeY)
        // With vertical tangents at both ends (dx = 0)
        double c1y = startY + (nodeY - startY) * 0.45;
        double c2y = startY + (nodeY - startY) * 0.55;
        figure.Segments.Add(new BezierSegment(
            new Point(midX, c1y),
            new Point(nodeX, c2y),
            pNode,
            true));

        // Curve segment 2: from pNode (nodeX, nodeY) to pEnd (midX, endY)
        // With vertical tangents at both ends
        if (!IsLast || endY > nodeY)
        {
            double c3y = nodeY + (endY - nodeY) * 0.45;
            double c4y = nodeY + (endY - nodeY) * 0.55;
            figure.Segments.Add(new BezierSegment(
                new Point(nodeX, c3y),
                new Point(midX, c4y),
                pEnd,
                true));
        }

        pathGeometry.Figures.Add(figure);
        pathGeometry.Freeze();

        // Draw glow and core curve
        dc.DrawGeometry(null, glowPen, pathGeometry);
        dc.DrawGeometry(null, corePen, pathGeometry);

        // First row start cap
        if (IsFirst)
        {
            var startBrush = new SolidColorBrush(accentColor);
            startBrush.Freeze();
            dc.DrawEllipse(startBrush, null, pStart, 5.0, 5.0);
        }

        // Last row end cap
        if (IsLast)
        {
            var endBrush = new SolidColorBrush(accentColor);
            endBrush.Freeze();
            dc.DrawEllipse(endBrush, null, pEnd, 4.0, 4.0);
        }

        // 3. Node on the curve (Red/Crimson dot on bends as in sketch, Golden for Milestones)
        Color auraColor = IsMilestone
            ? Color.FromArgb(90, 245, 166, 35)
            : Color.FromArgb(70, accentColor.R, accentColor.G, accentColor.B);
        var auraBrush = new SolidColorBrush(auraColor);
        auraBrush.Freeze();
        dc.DrawEllipse(auraBrush, null, pNode, 11.0, 11.0);

        Color ringColor = IsMilestone ? Color.FromRgb(245, 166, 35) : accentColor;
        var nodeRingPen = new Pen(new SolidColorBrush(ringColor), 2.0);
        nodeRingPen.Freeze();

        var nodeBgBrush = new SolidColorBrush(Color.FromRgb(26, 26, 46));
        nodeBgBrush.Freeze();
        dc.DrawEllipse(nodeBgBrush, nodeRingPen, pNode, 7.0, 7.0);

        Color centerColor = IsMilestone ? Color.FromRgb(245, 166, 35) : Colors.White;
        var centerBrush = new SolidColorBrush(centerColor);
        centerBrush.Freeze();
        dc.DrawEllipse(centerBrush, null, pNode, 3.5, 3.5);
    }
}
