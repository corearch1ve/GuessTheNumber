namespace GuessTheNumber;

public partial class MainPage : ContentPage
{
    // Загаданное число
    private int secretNumber;

    // Текущее количество сделанных попыток
    private int attempts;

    // Максимальное число диапазона (меняется в зависимости от сложности)
    private int maxNumber = 10;

    // Секунды с начала игры
    private int elapsedSeconds = 0;

    // Таймер который тикает каждую секунду
    private IDispatcherTimer gameTimer;

    // Рекорды для каждой сложности (int.MaxValue = рекорда ещё нет)
    // Загружаются из Preferences при запуске и сохраняются при обновлении
    private int recordEasy;
    private int recordMedium;
    private int recordHard;

    // true = Free режим, false = Challenge режим
    private bool isFreeMode = true;

    // Максимум попыток в Challenge режиме (зависит от сложности)
    private int maxAttempts;

    // true = тёмная тема (по умолчанию), false = светлая
    private bool isDarkTheme = true;

    public MainPage()
    {
        InitializeComponent();

        // Загружаем рекорды из постоянного хранилища
        // Preferences сохраняет данные между запусками приложения
        LoadRecords();

        gameTimer = Dispatcher.CreateTimer();
        gameTimer.Interval = TimeSpan.FromSeconds(1);
        gameTimer.Tick += OnTimerTick;

        // Устанавливаем лёгкий режим по умолчанию при запуске
        SetActiveDifficulty("easy");

        StartNewGame();
    }

    // -------------------------------------------------------
    // РЕКОРДЫ — сохранение и загрузка через Preferences
    // -------------------------------------------------------

    // Загружает рекорды из хранилища при старте приложения
    private void LoadRecords()
    {
        recordEasy = Preferences.Get("record_easy", int.MaxValue);
        recordMedium = Preferences.Get("record_medium", int.MaxValue);
        recordHard = Preferences.Get("record_hard", int.MaxValue);
    }

    // Сохраняет рекорд в хранилище чтобы не сбрасывался при перезапуске
    private void SetCurrentRecord(int value)
    {
        if (maxNumber == 10)
        {
            recordEasy = value;
            Preferences.Set("record_easy", value);
        }
        else if (maxNumber == 100)
        {
            recordMedium = value;
            Preferences.Set("record_medium", value);
        }
        else
        {
            recordHard = value;
            Preferences.Set("record_hard", value);
        }
    }

    // Возвращает рекорд для текущего диапазона
    private int GetCurrentRecord()
    {
        return maxNumber switch
        {
            10 => recordEasy,
            100 => recordMedium,
            _ => recordHard
        };
    }

    // Обновляет лейбл рекорда для текущей сложности
    private void UpdateRecordLabel()
    {
        int record = GetCurrentRecord();
        RecordLabel.Text = record == int.MaxValue ? "—" : $"{record} поп.";
    }

    // -------------------------------------------------------
    // ТЕМА — переключение светлая / тёмная
    // -------------------------------------------------------

    private void OnThemeToggleClicked(object sender, EventArgs e)
    {
        isDarkTheme = !isDarkTheme;
        ApplyTheme();
    }

    // Применяет цвета темы ко всем элементам страницы
    private void ApplyTheme()
    {
        if (isDarkTheme)
        {
            // Тёмная тема
            ThemeButton.Text = "☀️";
            BackgroundColor = Color.FromArgb("#0D0D0D");

            DifficultyLabel.TextColor = Color.FromArgb("#888888");
            ModeLabel.TextColor = Color.FromArgb("#666666");
            TimerLabel.TextColor = Colors.White;
            AttemptsLabel.TextColor = Colors.White;
            HintLabel.TextColor = Colors.White;
            HistoryTitleLabel.TextColor = Color.FromArgb("#444444");

            GuessEntry.BackgroundColor = Color.FromArgb("#1A1A1A");
            GuessEntry.TextColor = Colors.White;
            GuessEntry.PlaceholderColor = Color.FromArgb("#444444");

            NewGameButton.BackgroundColor = Color.FromArgb("#1A1A1A");
        }
        else
        {
            // Светлая тема
            ThemeButton.Text = "🌙";
            BackgroundColor = Color.FromArgb("#F5F5F5");

            DifficultyLabel.TextColor = Color.FromArgb("#555555");
            ModeLabel.TextColor = Color.FromArgb("#777777");
            TimerLabel.TextColor = Color.FromArgb("#111111");
            AttemptsLabel.TextColor = Color.FromArgb("#111111");
            HintLabel.TextColor = Color.FromArgb("#111111");
            HistoryTitleLabel.TextColor = Color.FromArgb("#999999");

            GuessEntry.BackgroundColor = Color.FromArgb("#E0E0E0");
            GuessEntry.TextColor = Color.FromArgb("#111111");
            GuessEntry.PlaceholderColor = Color.FromArgb("#AAAAAA");

            NewGameButton.BackgroundColor = Color.FromArgb("#E0E0E0");
        }

        // Обновляем подсветку кнопок сложности под новую тему
        RefreshDifficultyButtons();
    }

    // Перерисовывает кнопки сложности с учётом активной темы
    private void RefreshDifficultyButtons()
    {
        string active = maxNumber switch
        {
            10 => "easy",
            100 => "medium",
            _ => "hard"
        };
        SetActiveDifficulty(active);
    }

    // -------------------------------------------------------
    // ИГРОВАЯ ЛОГИКА
    // -------------------------------------------------------

    private void OnTimerTick(object sender, EventArgs e)
    {
        elapsedSeconds++;
        TimerLabel.Text = $"{elapsedSeconds} сек";
    }

    private void StartNewGame()
    {
        var random = new Random();
        secretNumber = random.Next(1, maxNumber + 1);

        // Сбрасываем счётчики
        attempts = 0;
        elapsedSeconds = 0;

        // Сбрасываем UI
        AttemptsLabel.Text = "0";
        HintLabel.Text = "";
        GuessEntry.Text = "";
        TimerLabel.Text = "0 сек";
        HistoryLayout.Children.Clear();
        HistoryTitleLabel.IsVisible = false;
        DifficultyLabel.Text = $"Я загадал число от 1 до {maxNumber}";

        // Блокируем ввод до нажатия "Начать игру"
        GuessButton.IsEnabled = false;
        GuessEntry.IsEnabled = false;

        // Показываем "Начать", прячем "Новая игра"
        StartButton.IsVisible = true;
        NewGameButton.IsVisible = false;

        // Лимит попыток: Easy = 5, Medium = 7, Hard = 10
        maxAttempts = maxNumber switch
        {
            10 => 5,
            100 => 7,
            1000 => 10,
            _ => 7
        };

        // Показываем прогресс-бар только в Challenge режиме
        if (isFreeMode)
            ChallengeProgressLayout.IsVisible = false;
        else
        {
            ChallengeProgressLayout.IsVisible = true;
            UpdateAttemptsProgress();
        }

        // Таймер стартует только по кнопке "Начать игру"
        gameTimer.Stop();

        UpdateRecordLabel();
    }

    // Обновляет прогресс-бар и текст оставшихся попыток
    private void UpdateAttemptsProgress()
    {
        int attemptsLeft = maxAttempts - attempts;
        AttemptsLeftLabel.Text = $"Осталось попыток: {attemptsLeft} / {maxAttempts}";

        // Прогресс от 1.0 (полный) до 0.0 (пустой)
        double progress = (double)attemptsLeft / maxAttempts;
        AttemptsProgressBar.Progress = progress;

        // Цвет меняется: зелёный → оранжевый → красный
        AttemptsProgressBar.ProgressColor = progress switch
        {
            > 0.6 => Color.FromArgb("#00FF88"),
            > 0.3 => Color.FromArgb("#FF8800"),
            _ => Color.FromArgb("#FF4444")
        };
    }

    // -------------------------------------------------------
    // КНОПКИ СЛОЖНОСТИ
    // -------------------------------------------------------

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

    // Подсвечивает активную кнопку сложности, остальные сбрасывает
    private void SetActiveDifficulty(string level)
    {
        // Цвет фона неактивных кнопок зависит от темы
        string inactiveBg = isDarkTheme ? "#1A1A1A" : "#E0E0E0";

        EasyButton.BackgroundColor = Color.FromArgb(inactiveBg);
        EasyButton.BorderColor = Color.FromArgb("#444444");
        EasyButton.BorderWidth = 1;
        MediumButton.BackgroundColor = Color.FromArgb(inactiveBg);
        MediumButton.BorderColor = Color.FromArgb("#444444");
        MediumButton.BorderWidth = 1;
        HardButton.BackgroundColor = Color.FromArgb(inactiveBg);
        HardButton.BorderColor = Color.FromArgb("#444444");
        HardButton.BorderWidth = 1;

        // Активная кнопка подсвечивается своим цветом
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

    // -------------------------------------------------------
    // КНОПКИ РЕЖИМА
    // -------------------------------------------------------

    private void OnFreeModeClicked(object sender, EventArgs e)
    {
        isFreeMode = true;
        ModeLabel.Text = "Режим: Free — неограниченные попытки";

        FreeModeButton.BackgroundColor = Color.FromArgb("#1A1A2A");
        FreeModeButton.BorderColor = Color.FromArgb("#AAAAFF");
        FreeModeButton.BorderWidth = 2;
        ChallengeModeButton.BackgroundColor = Color.FromArgb("#1A1A1A");
        ChallengeModeButton.BorderColor = Color.FromArgb("#444444");
        ChallengeModeButton.BorderWidth = 1;

        StartNewGame();
    }

    private void OnChallengeModeClicked(object sender, EventArgs e)
    {
        isFreeMode = false;
        ModeLabel.Text = "Режим: Challenge — попытки ограничены!";

        ChallengeModeButton.BackgroundColor = Color.FromArgb("#2A1A0A");
        ChallengeModeButton.BorderColor = Color.FromArgb("#FF8800");
        ChallengeModeButton.BorderWidth = 2;
        FreeModeButton.BackgroundColor = Color.FromArgb("#1A1A1A");
        FreeModeButton.BorderColor = Color.FromArgb("#444444");
        FreeModeButton.BorderWidth = 1;

        StartNewGame();
    }

    // -------------------------------------------------------
    // ОСНОВНАЯ ЛОГИКА ПРОВЕРКИ ЧИСЛА
    // -------------------------------------------------------

    private async void OnGuessClicked(object sender, EventArgs e)
    {
        // Проверяем что поле не пустое
        if (string.IsNullOrWhiteSpace(GuessEntry.Text))
        {
            HintLabel.Text = "Введи число!";
            HintLabel.TextColor = Colors.Orange;
            // Анимация тряски кнопки при ошибке ввода
            await ShakeAnimation(GuessButton);
            return;
        }

        // Проверяем что введено именно число
        if (!int.TryParse(GuessEntry.Text, out int userGuess))
        {
            HintLabel.Text = "Это не число!";
            HintLabel.TextColor = Colors.Orange;
            await ShakeAnimation(GuessButton);
            return;
        }

        // Проверяем что число в допустимом диапазоне
        if (userGuess < 1 || userGuess > maxNumber)
        {
            HintLabel.Text = $"Число должно быть от 1 до {maxNumber}!";
            HintLabel.TextColor = Colors.Orange;
            await ShakeAnimation(GuessButton);
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
            // Игрок угадал! Останавливаем таймер
            gameTimer.Stop();

            bool isNewRecord = attempts < GetCurrentRecord();
            if (isNewRecord)
            {
                SetCurrentRecord(attempts); // Сохраняет в Preferences автоматически
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
            NewGameButton.IsVisible = true;

            AddHistoryItem(userGuess, arrow, arrowColor);
            GuessEntry.Text = "";

            // Анимация победы — кнопка пульсирует зелёным
            await WinAnimation(isNewRecord);
            return;
        }

        // В Challenge режиме проверяем не закончились ли попытки
        if (!isFreeMode)
        {
            UpdateAttemptsProgress();

            if (attempts >= maxAttempts)
            {
                gameTimer.Stop();
                HintLabel.Text = $"💀 Попытки кончились! Было загадано: {secretNumber}";
                HintLabel.TextColor = Color.FromArgb("#FF4444");
                GuessButton.IsEnabled = false;
                NewGameButton.IsVisible = true;

                // Анимация поражения — экран вспыхивает красным
                await LoseAnimation();

                AddHistoryItem(userGuess, arrow, arrowColor);
                GuessEntry.Text = "";
                return;
            }
        }

        AddHistoryItem(userGuess, arrow, arrowColor);
        GuessEntry.Text = "";
    }

    // -------------------------------------------------------
    // АНИМАЦИИ
    // -------------------------------------------------------

    // Тряска элемента при ошибке ввода
    private async Task ShakeAnimation(View view)
    {
        for (int i = 0; i < 3; i++)
        {
            await view.TranslateTo(-8, 0, 50);
            await view.TranslateTo(8, 0, 50);
        }
        await view.TranslateTo(0, 0, 50);
    }

    // Победная анимация — кнопка увеличивается и возвращается
    // При новом рекорде ещё и вспыхивает золотым
    private async Task WinAnimation(bool isNewRecord)
    {
        if (isNewRecord)
        {
            // Золотая вспышка при рекорде
            var originalBg = BackgroundColor;
            BackgroundColor = Color.FromArgb("#1A1500");
            await Task.Delay(150);
            BackgroundColor = originalBg;
            await Task.Delay(100);
            BackgroundColor = Color.FromArgb("#1A1500");
            await Task.Delay(150);
            BackgroundColor = originalBg;
        }

        // Кнопка "Новая игра" появляется с пульсацией
        NewGameButton.Scale = 0.8;
        await NewGameButton.ScaleTo(1.1, 200);
        await NewGameButton.ScaleTo(1.0, 100);
    }

    // Анимация поражения — экран вспыхивает красным
    private async Task LoseAnimation()
    {
        var originalBg = BackgroundColor;
        BackgroundColor = Color.FromArgb("#1A0000");
        await Task.Delay(200);
        BackgroundColor = originalBg;
        await Task.Delay(100);
        BackgroundColor = Color.FromArgb("#1A0000");
        await Task.Delay(200);
        BackgroundColor = originalBg;
    }

    // -------------------------------------------------------
    // ИСТОРИЯ ПОПЫТОК
    // -------------------------------------------------------

    // Добавляет строку в историю попыток
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
            TextColor = isDarkTheme ? Colors.White : Color.FromArgb("#111111"),
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

        // Вставляем в начало — последняя попытка сверху
        HistoryLayout.Children.Insert(0, row);
    }

    // -------------------------------------------------------
    // КНОПКИ СТАРТ / НОВАЯ ИГРА
    // -------------------------------------------------------

    private void OnStartGameClicked(object sender, EventArgs e)
    {
        // Запускаем таймер и разблокируем ввод
        gameTimer.Start();
        GuessButton.IsEnabled = true;
        GuessEntry.IsEnabled = true;

        // Прячем кнопку "Начать игру"
        StartButton.IsVisible = false;
    }

    private void OnNewGameClicked(object sender, EventArgs e)
    {
        StartNewGame();
    }
}
