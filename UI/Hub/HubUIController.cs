using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HubUIController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private TaskCatalog catalog;

    [Header("UI Assets")]
    [SerializeField] private Texture2D logoTexture;

    private const string StudentLogin = "student";
    private const string StudentPassword = "12345";

    private const string AdminLogin = "admin";
    private const string AdminPassword = "12345";

    private VisualElement _root;

    private VisualElement _loginPanel;
    private VisualElement _appPanel;
    private VisualElement _studyView;
    private VisualElement _resultsView;
    private VisualElement _adminView;
    private VisualElement _errorOverlay;

    private TextField _loginField;
    private TextField _passwordField;

    private Label _profileName;
    private Label _errorMessage;
    private Label _resultsNote;

    private ScrollView _tasksList;
    private ScrollView _resultsAttemptsScroll;
    private ScrollView _adminAttemptsScroll;

    private Image _logoImage;

    private Button _loginButton;
    private Button _logoutButton;
    private Button _errorOkButton;
    private Button _tabStudyButton;
    private Button _tabResultsButton;
    private Button _tabAdminButton;

    private string _currentLogin = string.Empty;
    private UserRole _currentRole = UserRole.None;

    private readonly List<DemoAttempt> _demoAttempts = new()
    {
        // Личные результаты тестового студента student
        new DemoAttempt("student", "Иван Петров",    "МР25-01-1",  "АКБ и масса кузова",                 new DateTime(2026, 3, 12, 10, 25, 0), 172, 0, 5),
        new DemoAttempt("student", "Иван Петров",    "МР25-01-1",  "Плафон салонного освещения",        new DateTime(2026, 3, 12, 11, 10, 0), 244, 1, 4),
        new DemoAttempt("student", "Иван Петров",    "МР25-01-1",  "Концевик стоп-сигнала",             new DateTime(2026, 3, 13,  9, 40, 0), 198, 0, 5),
        new DemoAttempt("student", "Иван Петров",    "МР25-01-1",  "Блок включения фар",                new DateTime(2026, 3, 14, 13,  0, 0), 326, 2, 3),

        // Группа МР25-01-1
        new DemoAttempt("student_a", "Данил Смирнов", "МР25-01-1",  "Замок багажника",                  new DateTime(2026, 3, 13, 10, 55, 0), 281, 1, 4),
        new DemoAttempt("student_a", "Данил Смирнов", "МР25-01-1",  "Кнопка аварийной сигнализации",    new DateTime(2026, 3, 14,  9, 15, 0), 355, 2, 3),
        new DemoAttempt("student_b", "Егор Васильев", "МР25-01-1",  "АКБ и масса кузова",               new DateTime(2026, 3, 15, 11, 20, 0), 164, 0, 5),

        // Группа МР24-01-2
        new DemoAttempt("student_c", "Мария Соколова", "МР24-01-2", "Щиток приборов",                   new DateTime(2026, 3, 14, 11, 45, 0), 219, 0, 5),
        new DemoAttempt("student_c", "Мария Соколова", "МР24-01-2", "Цепь стеклоочистителей",          new DateTime(2026, 3, 15, 12, 30, 0), 401, 2, 3),
        new DemoAttempt("student_d", "Артем Кузнецов", "МР24-01-2", "Кнопка аварийной сигнализации",    new DateTime(2026, 3, 13, 12, 20, 0), 355, 3, 3),
        new DemoAttempt("student_d", "Артем Кузнецов", "МР24-01-2", "Диагностика подсветки номера",     new DateTime(2026, 3, 16, 10,  5, 0), 268, 1, 4),

        // Группа МР24-01-1П
        new DemoAttempt("student_e", "Елена Морозова", "МР24-01-1П", "Замок багажника",                 new DateTime(2026, 3, 16,  9, 50, 0), 236, 0, 5),
        new DemoAttempt("student_e", "Елена Морозова", "МР24-01-1П", "Плафон салонного освещения",      new DateTime(2026, 3, 16, 11, 40, 0), 252, 1, 4),
        new DemoAttempt("student_f", "Никита Орлов",   "МР24-01-1П", "Концевик стоп-сигнала",           new DateTime(2026, 3, 17,  8, 55, 0), 312, 2, 3),
        new DemoAttempt("student_f", "Никита Орлов",   "МР24-01-1П", "АКБ и масса кузова",              new DateTime(2026, 3, 17, 10, 15, 0), 188, 1, 4),

        // Личные тестовые результаты администратора
        new DemoAttempt("admin", "Администратор", "—", "Тестовый запуск: АКБ",          new DateTime(2026, 3, 15, 15, 30, 0), 155, 0, 5),
        new DemoAttempt("admin", "Администратор", "—", "Тестовый запуск: масса кузова", new DateTime(2026, 3, 15, 16, 10, 0), 210, 1, 4)
    };

    private void OnEnable()
    {
        var document = GetComponent<UIDocument>();
        _root = document.rootVisualElement;

        CacheReferences();
        ApplyLogo();
        RegisterCallbacks();

        ShowLogin();
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void CacheReferences()
    {
        _loginPanel = _root.Q<VisualElement>("login-panel");
        _appPanel = _root.Q<VisualElement>("app-panel");
        _studyView = _root.Q<VisualElement>("study-view");
        _resultsView = _root.Q<VisualElement>("results-view");
        _adminView = _root.Q<VisualElement>("admin-view");
        _errorOverlay = _root.Q<VisualElement>("error-overlay");

        _loginField = _root.Q<TextField>("login-login");
        _passwordField = _root.Q<TextField>("login-password");

        _profileName = _root.Q<Label>("profile-name");
        _errorMessage = _root.Q<Label>("error-message");
        _resultsNote = _root.Q<Label>("results-note");

        _tasksList = _root.Q<ScrollView>("tasks-list");
        _resultsAttemptsScroll = _root.Q<ScrollView>("results-attempts-scroll");
        _adminAttemptsScroll = _root.Q<ScrollView>("admin-attempts-scroll");

        _logoImage = _root.Q<Image>("logo-image");

        _loginButton = _root.Q<Button>("login-btn");
        _logoutButton = _root.Q<Button>("logout-btn");
        _errorOkButton = _root.Q<Button>("error-ok-btn");
        _tabStudyButton = _root.Q<Button>("tab-study-btn");
        _tabResultsButton = _root.Q<Button>("tab-results-btn");
        _tabAdminButton = _root.Q<Button>("tab-admin-btn");

        if (_passwordField != null)
            _passwordField.isPasswordField = true;
    }

    private void RegisterCallbacks()
    {
        if (_loginButton != null)
            _loginButton.clicked += OnLoginClicked;

        if (_logoutButton != null)
            _logoutButton.clicked += OnLogoutClicked;

        if (_errorOkButton != null)
            _errorOkButton.clicked += HideError;

        if (_tabStudyButton != null)
            _tabStudyButton.clicked += ShowStudyTab;

        if (_tabResultsButton != null)
            _tabResultsButton.clicked += ShowResultsTab;

        if (_tabAdminButton != null)
            _tabAdminButton.clicked += ShowAdminTab;
    }

    private void UnregisterCallbacks()
    {
        if (_loginButton != null)
            _loginButton.clicked -= OnLoginClicked;

        if (_logoutButton != null)
            _logoutButton.clicked -= OnLogoutClicked;

        if (_errorOkButton != null)
            _errorOkButton.clicked -= HideError;

        if (_tabStudyButton != null)
            _tabStudyButton.clicked -= ShowStudyTab;

        if (_tabResultsButton != null)
            _tabResultsButton.clicked -= ShowResultsTab;

        if (_tabAdminButton != null)
            _tabAdminButton.clicked -= ShowAdminTab;
    }

    private void ApplyLogo()
    {
        if (_logoImage == null)
            return;

        if (logoTexture != null)
        {
            _logoImage.image = logoTexture;
            _logoImage.scaleMode = ScaleMode.ScaleToFit;
        }
        else
        {
            _logoImage.image = null;
        }
    }

    private void OnLoginClicked()
    {
        HideError();

        string login = (_loginField?.value ?? string.Empty).Trim();
        string password = (_passwordField?.value ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(login) && string.IsNullOrWhiteSpace(password))
        {
            ShowError("Введите логин и пароль.");
            return;
        }

        if (string.IsNullOrWhiteSpace(login))
        {
            ShowError("Поле \"Логин\" не заполнено.");
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Поле \"Пароль\" не заполнено.");
            return;
        }

        if (!TryAuthorize(login, password, out var role))
        {
            ShowError("Неверный логин или пароль.");
            return;
        }

        _currentLogin = login;
        _currentRole = role;

        BuildTaskList();
        BuildResultsPanel();
        BuildAdminPanel();
        ShowApp();
    }

    private bool TryAuthorize(string login, string password, out UserRole role)
    {
        if (login == StudentLogin && password == StudentPassword)
        {
            role = UserRole.Student;
            return true;
        }

        if (login == AdminLogin && password == AdminPassword)
        {
            role = UserRole.Admin;
            return true;
        }

        role = UserRole.None;
        return false;
    }

    private void OnLogoutClicked()
    {
        if (_loginField != null) _loginField.value = string.Empty;
        if (_passwordField != null) _passwordField.value = string.Empty;

        ShowLogin();
    }

    private void ShowLogin()
    {
        _currentLogin = string.Empty;
        _currentRole = UserRole.None;

        _loginPanel.RemoveFromClassList("hidden");
        _appPanel.AddToClassList("hidden");

        _studyView.RemoveFromClassList("hidden");
        _resultsView.AddToClassList("hidden");
        _adminView.AddToClassList("hidden");

        _tabResultsButton?.AddToClassList("hidden");
        _tabAdminButton?.AddToClassList("hidden");

        DeactivateAllTabs();

        _tasksList?.Clear();
        _resultsAttemptsScroll?.Clear();
        _adminAttemptsScroll?.Clear();

        if (_profileName != null)
            _profileName.text = "Профиль: -";

        if (_resultsNote != null)
            _resultsNote.text = string.Empty;

        HideError();
    }

    private void ShowApp()
    {
        _loginPanel.AddToClassList("hidden");
        _appPanel.RemoveFromClassList("hidden");

        if (_profileName != null)
            _profileName.text = $"Профиль: {GetRoleLabel(_currentRole)} | {_currentLogin}";

        _tabResultsButton?.RemoveFromClassList("hidden");

        if (_currentRole == UserRole.Admin)
            _tabAdminButton?.RemoveFromClassList("hidden");
        else
            _tabAdminButton?.AddToClassList("hidden");

        ShowStudyTab();
    }

    private void ShowStudyTab()
    {
        _studyView?.RemoveFromClassList("hidden");
        _resultsView?.AddToClassList("hidden");
        _adminView?.AddToClassList("hidden");

        DeactivateAllTabs();
        _tabStudyButton?.AddToClassList("tabButtonActive");
    }

    private void ShowResultsTab()
    {
        _studyView?.AddToClassList("hidden");
        _resultsView?.RemoveFromClassList("hidden");
        _adminView?.AddToClassList("hidden");

        DeactivateAllTabs();
        _tabResultsButton?.AddToClassList("tabButtonActive");
    }

    private void ShowAdminTab()
    {
        if (_currentRole != UserRole.Admin)
            return;

        _studyView?.AddToClassList("hidden");
        _resultsView?.AddToClassList("hidden");
        _adminView?.RemoveFromClassList("hidden");

        DeactivateAllTabs();
        _tabAdminButton?.AddToClassList("tabButtonActive");
    }

    private void DeactivateAllTabs()
    {
        _tabStudyButton?.RemoveFromClassList("tabButtonActive");
        _tabResultsButton?.RemoveFromClassList("tabButtonActive");
        _tabAdminButton?.RemoveFromClassList("tabButtonActive");
    }

    private void BuildTaskList()
    {
        if (_tasksList == null)
            return;

        _tasksList.Clear();

        if (catalog == null || catalog.tasks == null || catalog.tasks.Count == 0)
        {
            AddEmptyState(_tasksList, "Нет заданий. Заполните TaskCatalog.");
            return;
        }

        foreach (var task in catalog.tasks)
        {
            var card = new VisualElement();
            card.AddToClassList("taskCard");

            var topRow = new VisualElement();
            topRow.AddToClassList("taskTopRow");

            var info = new VisualElement();
            info.AddToClassList("taskInfo");

            var title = new Label(task.title);
            title.AddToClassList("taskTitle");
            info.Add(title);

            if (!string.IsNullOrWhiteSpace(task.description))
            {
                var desc = new Label(task.description);
                desc.AddToClassList("taskDesc");
                info.Add(desc);
            }

            var button = new Button(() => LoadTaskScene(task.sceneName))
            {
                text = "Запустить"
            };
            button.AddToClassList("taskLaunchBtn");

            topRow.Add(info);
            topRow.Add(button);

            card.Add(topRow);
            _tasksList.Add(card);
        }
    }

    private void BuildResultsPanel()
    {
        if (_resultsAttemptsScroll == null)
            return;

        _resultsAttemptsScroll.Clear();

        var attempts = _demoAttempts
            .Where(a => string.Equals(a.Login, _currentLogin, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.CompletedAt)
            .ToList();

        if (_resultsNote != null)
        {
            _resultsNote.text = _currentRole == UserRole.Student
                ? "Показаны только ваши результаты."
                : "Показаны ваши тестовые результаты.";
        }

        if (attempts.Count == 0)
        {
            AddEmptyState(_resultsAttemptsScroll, "Результаты отсутствуют.");
            return;
        }

        foreach (var attempt in attempts)
        {
            _resultsAttemptsScroll.Add(CreatePersonalResultRow(attempt));
        }
    }

    private void BuildAdminPanel()
    {
        if (_adminAttemptsScroll == null)
            return;

        _adminAttemptsScroll.Clear();

        if (_currentRole != UserRole.Admin)
            return;

        var attempts = _demoAttempts
            .Where(a => !string.Equals(a.Login, AdminLogin, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.CompletedAt)
            .ToList();

        if (attempts.Count == 0)
        {
            AddEmptyState(_adminAttemptsScroll, "Нет данных по студентам.");
            return;
        }

        foreach (var attempt in attempts)
        {
            _adminAttemptsScroll.Add(CreateAdminResultRow(attempt));
        }
    }

    private VisualElement CreatePersonalResultRow(DemoAttempt attempt)
    {
        var row = new VisualElement();
        row.AddToClassList("tableDataRow");

        row.Add(CreateTextCell(attempt.ScenarioTitle, "colScenario"));
        row.Add(CreateTextCell(attempt.CompletedAt.ToString("dd.MM.yyyy HH:mm"), "colDate"));
        row.Add(CreateTextCell(FormatDuration(attempt.DurationSeconds), "colDuration"));
        row.Add(CreateTextCell(attempt.Errors.ToString(), "colErrors"));
        row.Add(CreateGradeCell(attempt.Grade));

        return row;
    }

    private VisualElement CreateAdminResultRow(DemoAttempt attempt)
    {
        var row = new VisualElement();
        row.AddToClassList("tableDataRow");

        row.Add(CreateTextCell(attempt.StudentName, "colStudent"));
        row.Add(CreateTextCell(attempt.GroupName, "colGroup"));
        row.Add(CreateTextCell(attempt.ScenarioTitle, "colScenario"));
        row.Add(CreateTextCell(attempt.CompletedAt.ToString("dd.MM.yyyy HH:mm"), "colDate"));
        row.Add(CreateTextCell(FormatDuration(attempt.DurationSeconds), "colDuration"));
        row.Add(CreateTextCell(attempt.Errors.ToString(), "colErrors"));
        row.Add(CreateGradeCell(attempt.Grade));

        return row;
    }

    private VisualElement CreateTextCell(string text, string columnClass)
    {
        var label = new Label(text);
        label.AddToClassList("tableCell");
        label.AddToClassList(columnClass);
        return label;
    }

    private VisualElement CreateGradeCell(int grade)
    {
        var container = new VisualElement();
        container.AddToClassList("tableCell");
        container.AddToClassList("colGrade");

        var pill = new Label($"Оценка {grade}");
        pill.AddToClassList("gradePill");

        container.Add(pill);
        return container;
    }

    private void AddEmptyState(ScrollView scrollView, string text)
    {
        var label = new Label(text);
        label.AddToClassList("emptyState");
        scrollView.Add(label);
    }

    private void ShowError(string message)
    {
        if (_errorMessage != null)
            _errorMessage.text = message;

        _errorOverlay?.RemoveFromClassList("hidden");
    }

    private void HideError()
    {
        _errorOverlay?.AddToClassList("hidden");
    }

    private string GetRoleLabel(UserRole role)
    {
        return role switch
        {
            UserRole.Student => "Студент",
            UserRole.Admin => "Администратор",
            _ => "-"
        };
    }

    private static string FormatDuration(int seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);

        if (time.TotalHours >= 1)
            return time.ToString(@"hh\:mm\:ss");

        return time.ToString(@"mm\:ss");
    }

    private void LoadTaskScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        SceneManager.LoadScene(sceneName);
    }

    private enum UserRole
    {
        None,
        Student,
        Admin
    }

    private sealed class DemoAttempt
    {
        public string Login { get; }
        public string StudentName { get; }
        public string GroupName { get; }
        public string ScenarioTitle { get; }
        public DateTime CompletedAt { get; }
        public int DurationSeconds { get; }
        public int Errors { get; }
        public int Grade { get; }

        public DemoAttempt(
            string login,
            string studentName,
            string groupName,
            string scenarioTitle,
            DateTime completedAt,
            int durationSeconds,
            int errors,
            int grade)
        {
            Login = login;
            StudentName = studentName;
            GroupName = groupName;
            ScenarioTitle = scenarioTitle;
            CompletedAt = completedAt;
            DurationSeconds = durationSeconds;
            Errors = errors;
            Grade = grade;
        }
    }
}