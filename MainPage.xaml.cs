namespace GuessTheNumber;

public partial class MainPage : ContentPage
{
    private int secretNumber;
    private int attempts;
    private int maxNumber = 100;

    private int elapsedSeconds = 0;
    private IDispatcherTimer gameTimer;

    private int recordEasy = int.MaxValue;
    private int recordMedium = int.MaxValue;
    private int recordHard = int.MaxValue;

    public MainPage()
    {
        InitializeComponent();
        gameTimer = Dispatcher.CreateTimer();
        gameTimer.Interval = TimeSpan.FromSeconds(1);
        gameTimer.Tick += OnTimerTick;
        StartNewGame();
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        elapsedSeconds++;
        TimerLabel.Text = $"{elapsedSeconds} сек";
    }

    private void StartNewGame()
    {
        Random random = new Random();
        secretNumber = random.Next(1, maxNumber + 1);
        attempts = 0;
        elapsedSeconds = 0;

        AttemptsLabel.Text = "0";
        HintLabel.Text = "";
        GuessEntry.Text = "";
        TimerLabel.Text = "0 сек";
        GuessButton.IsEnabled = true;
        HistoryLayout.Children.Clear();
        HistoryTitleLabel.IsVisible = false;
        DifficultyLabel.Text = $"Я загадал число от 1 до {maxNumber}";

        gameTimer.Stop();
        gameTimer.Start();

        UpdateRecordLabel();
    }

    private void UpdateRecordLabel()
    {
        int record = GetCurrentRecord();
        RecordLabel.Text = record == int.MaxValue ? "—" : $"{record} поп.";
    }

    private int GetCurrentRecord()
    {
        if (maxNumber == 10) return recordEasy;
        if (maxNumber == 100) return recordMedium;
        return recordHard;
    }

    private void SetCurrentRecord(int value)
    {
        if (maxNumber == 10) recordEasy = value;
        else if (maxNumber == 100) recordMedium = value;
        else recordHard = value;
    }

    private void OnEasyClicked(object sender, EventArgs e)
    {
        maxNumber = 10;
        SetActiveDifficulty("easy");
        StartNewGame();
    }

    private void OnMediumClicked(object sender, EventArgs e)
    {
        maxNumber = 100;
        SetActiveDifficulty("medium");
        StartNewGame();
    }

    private void OnHardClicked(object sender, EventArgs e)
    {
        maxNumber = 1000;
        SetActiveDifficulty("hard");
        StartNewGame();
    }

    private void SetActiveDifficulty(string level)
    {
        EasyButton.BackgroundColor = Color.FromArgb("#1A1A1A");
        EasyButton.BorderColor = Color.FromArgb("#444444");
        EasyButton.BorderWidth = 1;

        MediumButton.BackgroundColor = Color.FromArgb("#1A1A1A");
        MediumButton.BorderColor = Color.FromArgb("#444444");
        MediumButton.BorderWidth = 1;

        HardButton.BackgroundColor = Color.FromArgb("#1A1A1A");
        HardButton.BorderColor = Color.FromArgb("#444444");
        HardButton.BorderWidth = 1;

        if (level == "easy")
        {
            EasyButton.BackgroundColor = Color.FromArgb("#1A3A1A");
            EasyButton.BorderColor = Color.FromArgb("#00FF88");
            EasyButton.BorderWidth = 2;
        }
        else if (level == "medium")
        {
            MediumButton.BackgroundColor = Color.FromArgb("#3A3A1A");
            MediumButton.BorderColor = Color.FromArgb("#FFD700");
            MediumButton.BorderWidth = 2;
        }
        else if (level == "hard")
        {
            HardButton.BackgroundColor = Color.FromArgb("#3A1A1A");
            HardButton.BorderColor = Color.FromArgb("#FF4444");
            HardButton.BorderWidth = 2;
        }
    }

    private void OnGuessClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(GuessEntry.Text))
        {
            HintLabel.Text = "Введи число!";
            HintLabel.TextColor = Colors.Orange;
            return;
        }

        if (!int.TryParse(GuessEntry.Text, out int userGuess))
        {
            HintLabel.Text = "Это не число!";
            HintLabel.TextColor = Colors.Orange;
            return;
        }

        if (userGuess < 1 || userGuess > maxNumber)
        {
            HintLabel.Text = $"Число должно быть от 1 до {maxNumber}!";
            HintLabel.TextColor = Colors.Orange;
            return;
        }

        attempts++;
        AttemptsLabel.Text = $"{attempts}";
        HistoryTitleLabel.IsVisible = true;

        string arrow;
        Color arrowColor;

        if (userGuess < secretNumber)
        {
            HintLabel.Text = "⬆️ Больше!";
            HintLabel.TextColor = Color.FromArgb("#00AAFF");
            arrow = "⬆ больше";
            arrowColor = Color.FromArgb("#00AAFF");
        }
        else if (userGuess > secretNumber)
        {
            HintLabel.Text = "⬇️ Меньше!";
            HintLabel.TextColor = Color.FromArgb("#FF4444");
            arrow = "⬇ меньше";
            arrowColor = Color.FromArgb("#FF4444");
        }
        else
        {
            gameTimer.Stop();

            int currentRecord = GetCurrentRecord();
            bool isNewRecord = attempts < currentRecord;

            if (isNewRecord)
            {
                SetCurrentRecord(attempts);
                HintLabel.Text = $"🏆 Новый рекорд! {attempts} поп. за {elapsedSeconds} сек!";
                HintLabel.TextColor = Color.FromArgb("#FFD700");
                RecordLabel.Text = $"{attempts} поп.";
            }
            else
            {
                HintLabel.Text = $"🎉 Угадал за {attempts} поп. и {elapsedSeconds} сек!";
                HintLabel.TextColor = Color.FromArgb("#00FF88");
            }

            arrow = "✅ угадал!";
            arrowColor = Color.FromArgb("#00FF88");
            GuessButton.IsEnabled = false;
            AddHistoryItem(userGuess, arrow, arrowColor);
            GuessEntry.Text = "";
            return;
        }

        AddHistoryItem(userGuess, arrow, arrowColor);
        GuessEntry.Text = "";
    }

    private void AddHistoryItem(int number, string arrow, Color color)
    {
        var row = new HorizontalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = LayoutOptions.Center
        };

        var numberLabel = new Label
        {
            Text = number.ToString(),
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            WidthRequest = 50,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var arrowLabel = new Label
        {
            Text = arrow,
            FontSize = 16,
            TextColor = color
        };

        row.Children.Add(numberLabel);
        row.Children.Add(arrowLabel);
        HistoryLayout.Children.Insert(0, row);
    }

    private void OnNewGameClicked(object sender, EventArgs e)
    {
        StartNewGame();
    }
}