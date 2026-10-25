using SubTrack.WinForms.Themes;

namespace SubTrack.WinForms.Controls;

internal class EmptyStateView : UserControl
{
    public EmptyStateView(
        string title,
        string description,
        string symbol,
        string? actionText = null,
        EventHandler? onActionClick = null,
        string? emptyTitleText = null,
        string? emptyDescriptionText = null)
    {
        Dock = DockStyle.Fill;
        BackColor = AppTheme.WindowBackground;
        Padding = new Padding(48, 40, 48, 32);

        var heading = new Label
        {
            AutoSize = true,
            Font = AppTheme.TitleFont,
            ForeColor = AppTheme.TextPrimary,
            Margin = Padding.Empty,
            Text = title
        };

        var subtitle = new Label
        {
            AutoSize = true,
            Font = AppTheme.SubtitleFont,
            ForeColor = AppTheme.TextSecondary,
            Margin = new Padding(0, 6, 0, 0),
            Text = description
        };

        var emptySymbol = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 35F, FontStyle.Regular),
            ForeColor = AppTheme.Primary,
            Margin = new Padding(0, 0, 0, 14),
            Text = symbol
        };

        var emptyTitle = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Margin = new Padding(0, 0, 0, 7),
            Text = emptyTitleText ?? "Aún no hay información para mostrar"
        };

        var emptyDescription = new Label
        {
            AutoSize = true,
            Font = AppTheme.SubtitleFont,
            ForeColor = AppTheme.TextSecondary,
            MaximumSize = new Size(460, 0),
            Text = emptyDescriptionText ?? "Esta sección estará lista para mostrar información cuando se conecten los servicios de la aplicación.",
            TextAlign = ContentAlignment.MiddleCenter
        };

        var header = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            Margin = Padding.Empty,
            WrapContents = false
        };
        header.Controls.Add(heading);
        header.Controls.Add(subtitle);

        var emptyCard = new TableLayoutPanel
        {
            Anchor = AnchorStyles.None,
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Height = 280,
            Margin = Padding.Empty,
            Padding = new Padding(40, 24, 40, 24),
            RowCount = 3,
            Width = 560
        };
        emptyCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        emptyCard.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        emptyCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        emptyCard.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        var message = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.None,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            Margin = Padding.Empty,
            WrapContents = false
        };
        emptySymbol.Anchor = AnchorStyles.None;
        emptyTitle.Anchor = AnchorStyles.None;
        emptyDescription.Anchor = AnchorStyles.None;
        message.Controls.Add(emptySymbol);
        message.Controls.Add(emptyTitle);
        message.Controls.Add(emptyDescription);

        if (!string.IsNullOrEmpty(actionText) && onActionClick is not null)
        {
            var actionBtn = new Button
            {
                Anchor = AnchorStyles.None,
                BackColor = AppTheme.Primary,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = AppTheme.NavigationActiveFont,
                ForeColor = AppTheme.Surface,
                Height = 36,
                Margin = new Padding(0, 14, 0, 0),
                TabStop = true,
                Text = actionText,
                UseVisualStyleBackColor = false,
                Width = 180
            };
            actionBtn.FlatAppearance.BorderSize = 0;
            actionBtn.Click += onActionClick;
            message.Controls.Add(actionBtn);
        }

        emptyCard.Controls.Add(message, 0, 1);

        var contentGrid = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 1
        };
        contentGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        contentGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        contentGrid.Controls.Add(emptyCard, 0, 0);

        Controls.Add(contentGrid);
        Controls.Add(header);
    }
}
