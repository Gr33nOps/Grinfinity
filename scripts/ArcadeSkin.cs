using Godot;

/// <summary>The menu's warm cartoon palette, adapted to readable game controls.</summary>
public static class ArcadeSkin
{
    public static readonly Color Ink = new("321e3d"), Cream = new("fff0ce"), Orange = new("f5a451"), Berry = new("ab4564"), Muted = new("c5a9bf");
    public static Font Font => GD.Load<Font>("res://fonts/LilitaOne.ttf");
    public static Theme Theme()
    {
        var theme = new Theme { DefaultFont = Font, DefaultFontSize = 28 };
        theme.SetColor("font_color", "Label", Cream);
        theme.SetColor("font_color", "Button", Cream);
        theme.SetColor("font_hover_color", "Button", Ink);
        theme.SetColor("font_focus_color", "Button", Cream);
        theme.SetStylebox("normal", "Button", Box(new Color("67364f"), new Color("c66e80"), 18, 3));
        theme.SetStylebox("hover", "Button", Box(Orange, Cream, 18, 3));
        theme.SetStylebox("pressed", "Button", Box(Berry, Cream, 18, 3));
        var focus=Box(new Color(0,0,0,0), Cream,18,3);focus.ShadowSize=0;focus.ShadowColor=Colors.Transparent;theme.SetStylebox("focus","Button",focus);
        theme.SetStylebox("normal", "LineEdit", Box(new Color("281b36"), Muted, 12, 2));
        theme.SetColor("font_color", "LineEdit", Cream);
        // Icons follow the text: cream, ink on the orange hover.
        theme.SetColor("icon_normal_color", "Button", Cream);
        theme.SetColor("icon_focus_color", "Button", Cream);
        theme.SetColor("icon_hover_color", "Button", Ink);
        theme.SetColor("icon_pressed_color", "Button", Cream);
        theme.SetColor("icon_hover_pressed_color", "Button", Cream);
        theme.SetIcon("arrow", "OptionButton", UiIcons.Get("dropdown"));
        theme.SetConstant("modulate_arrow", "OptionButton", 1);
        // Sliders: a dark groove, filled orange up to a little planet for a knob.
        var groove = new StyleBoxFlat { BgColor = new Color("281b36"), ContentMarginTop = 6, ContentMarginBottom = 6 };
        groove.SetCornerRadiusAll(8);
        var filled = new StyleBoxFlat { BgColor = Orange, ContentMarginTop = 6, ContentMarginBottom = 6 };
        filled.SetCornerRadiusAll(8);
        theme.SetStylebox("slider", "HSlider", groove);
        theme.SetStylebox("grabber_area", "HSlider", filled);
        theme.SetStylebox("grabber_area_highlight", "HSlider", filled);
        foreach (string state in new[] { "grabber", "grabber_highlight", "grabber_disabled" })
            theme.SetIcon(state, "HSlider", UiIcons.Get("knob"));
        return theme;
    }
    public static StyleBoxFlat Box(Color fill, Color border, int radius = 28, int line = 3) => new()
    {
        BgColor = fill, BorderColor = border, BorderWidthLeft = line, BorderWidthRight = line,
        BorderWidthTop = line, BorderWidthBottom = line,
        CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
        CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 14, ContentMarginBottom = 14,
        ShadowColor = new Color(0.06f,0.02f,0.09f,0.5f), ShadowSize = 8, ShadowOffset = new Vector2(0,8)
    };
    public static Label Label(string text, int size = 28, Color? color = null)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", Font);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? Cream);
        return label;
    }
    public static Button Button(string text, System.Action action, bool primary = false, string icon = null)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 70), MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        button.Theme = Theme();
        if (primary)
        {
            button.AddThemeStyleboxOverride("normal", Box(Orange, new Color("ffcd85"),18,3));
            // Ink in every state: cream on orange is too faint to read, and the
            // primary button is usually the focused one.
            foreach (string state in new[] { "font_color", "font_focus_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color",
                         "icon_normal_color", "icon_focus_color", "icon_hover_color", "icon_pressed_color", "icon_hover_pressed_color" })
                button.AddThemeColorOverride(state, Ink);
        }
        if (icon != null)
            UiIcons.On(button, icon);
        button.Pressed += action;
        return button;
    }
    public static void Fill(Control control) => control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    public static VBoxContainer Modal(Control owner, string title, float width = 690)
    {
        owner.Theme = Theme();
        Fill(owner);
        var scrim = new ColorRect { Color = new Color(0.055f,0.025f,0.08f,0.9f), MouseFilter = Control.MouseFilterEnum.Stop };
        owner.AddChild(scrim); Fill(scrim);
        var center = new CenterContainer(); owner.AddChild(center); Fill(center);
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(width,0) };
        var frame = Box(new Color("392339"), new Color("986077"), 36);
        frame.ContentMarginLeft = frame.ContentMarginRight = 36;
        frame.ContentMarginTop = 26; frame.ContentMarginBottom = 34;
        panel.AddThemeStyleboxOverride("panel", frame);
        center.AddChild(panel);
        var rows = new VBoxContainer(); rows.AddThemeConstantOverride("separation",18); panel.AddChild(rows);
        if (!string.IsNullOrEmpty(title))
        {
            var heading = Label(title, 52);
            rows.AddChild(heading);
            rows.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4), MouseFilter = Control.MouseFilterEnum.Ignore });
        }
        return rows;
    }
    public static TextureRect Icon(string name, float size = 64)
    {
        return new TextureRect { Texture = GD.Load<Texture2D>($"res://art/cosmic/icon_{name}.svg"),
            CustomMinimumSize = new Vector2(size,size), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore };
    }
    // Restyle existing supporting screens without breaking their bound node paths.
    public static void SupportingScreen(Control root)
    {
        root.Theme=Theme();
        root.GetNodeOrNull<Control>("Panel")?.Hide();
        var backdrop=new CosmicBackdrop();root.AddChild(backdrop);
        var layout=root.GetNodeOrNull<VBoxContainer>("Layout");if(layout==null)return;
        void Style(Node node)
        {
            if(node is Label label)
            {
                label.AddThemeFontSizeOverride("font_size",label.Name=="Title"?48:28);
                label.AddThemeColorOverride("font_color",label.Name=="Title"?Orange:Cream);
            }
            if(node is Button button)
            {
                button.Flat=false;button.Theme=Theme();button.AddThemeFontSizeOverride("font_size",26);
                foreach(string key in new[]{"font_color","font_hover_color","font_focus_color","font_pressed_color"})button.RemoveThemeColorOverride(key);
                button.CustomMinimumSize=new Vector2(button.CustomMinimumSize.X,52);
                if(button.ToggleMode&&button.Name=="Check")UiIcons.Switch(button);
                else if(button is not OptionButton&&UiIcons.For(button.Name) is string icon)UiIcons.On(button,icon);
            }
            if(node is LineEdit field){field.AddThemeFontSizeOverride("font_size",28);field.RightIcon=UiIcons.Get("pencil_field");}
            if(node is HBoxContainer row&&UiIcons.For(row.Name) is string mark)UiIcons.Lead(row,mark);
            if(node is BoxContainer box)box.AddThemeConstantOverride("separation",10);
            if(node is HBoxContainer && root is ControlsMenu && node.GetParent().Name=="Rows")((HBoxContainer)node).Alignment=BoxContainer.AlignmentMode.Center;
            foreach(Node child in node.GetChildren())Style(child);
        }
        Style(layout);
        // The screen's title and its hint line each get their icon.
        if(layout.GetNodeOrNull<Label>("Title") is Label title&&UiIcons.For(root.Name) is string heading)UiIcons.Beside(title,heading,44,Orange);
        if(layout.GetNodeOrNull<Label>("Hint") is Label hint){hint.AddThemeColorOverride("font_color",Muted);UiIcons.Beside(hint,"info",28,Muted);}
        layout.OffsetLeft=-550;layout.OffsetRight=550;
        float height=Mathf.Min(940,layout.GetCombinedMinimumSize().Y+24);
        layout.OffsetTop=-height/2;layout.OffsetBottom=height/2;
        var panel=new Panel {MouseFilter=Control.MouseFilterEnum.Ignore,AnchorLeft=.5f,AnchorRight=.5f,AnchorTop=.5f,AnchorBottom=.5f,
            OffsetLeft=-582,OffsetRight=582,OffsetTop=-height/2-26,OffsetBottom=height/2+26};
        panel.AddThemeStyleboxOverride("panel",Box(new Color("392339"),new Color("986077"),36));root.AddChild(panel);root.MoveChild(panel,root.GetChildren().IndexOf(layout));
    }
    public static void Pop(Control control)
    {
        control.PivotOffset = control.Size * .5f;
        control.Scale = Vector2.One * .94f;
        control.Modulate = new Color(1,1,1,0);
        var tween = control.CreateTween().SetParallel();
        tween.TweenProperty(control,"scale",Vector2.One,.22).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(control,"modulate:a",1f,.16);
    }
}
