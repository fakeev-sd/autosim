using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class HubController : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private DatabaseManager database;

    [Header("Scene mapping")]
    [SerializeField] private string defaultScenarioScene = "C1";
    [SerializeField] private ScenarioSceneLink[] scenarioScenes;

    private VisualElement root;
    private VisualElement loginPanel;
    private VisualElement appPanel;
    private VisualElement studyView;
    private VisualElement resultsView;
    private VisualElement adminView;
    private VisualElement errorOverlay;

    private TextField loginField;
    private TextField passwordField;
    private Label profileName;
    private Label errorMessage;
    private ScrollView tasksList;
    private ScrollView resultsAttemptsScroll;
    private ScrollView adminAttemptsScroll;

    private Button loginButton;
    private Button logoutButton;
    private Button tabStudyButton;
    private Button tabResultsButton;
    private Button tabAdminButton;
    private Button errorOkButton;

    private void OnEnable()
    {
        if (document == null)
            document = GetComponent<UIDocument>();

        if (database == null)
            database = DatabaseManager.Instance;

        root = document.rootVisualElement;

        loginPanel = root.Q<VisualElement>("login-panel");
        appPanel = root.Q<VisualElement>("app-panel");
        studyView = root.Q<VisualElement>("study-view");
        resultsView = root.Q<VisualElement>("results-view");
        adminView = root.Q<VisualElement>("admin-view");
        errorOverlay = root.Q<VisualElement>("error-overlay");

        loginField = root.Q<TextField>("login-login");
        passwordField = root.Q<TextField>("login-password");
        profileName = root.Q<Label>("profile-name");
        errorMessage = root.Q<Label>("error-message");
        tasksList = root.Q<ScrollView>("tasks-list");
        resultsAttemptsScroll = root.Q<ScrollView>("results-attempts-scroll");
        adminAttemptsScroll = root.Q<ScrollView>("admin-attempts-scroll");

        loginButton = root.Q<Button>("login-btn");
        logoutButton = root.Q<Button>("logout-btn");
        tabStudyButton = root.Q<Button>("tab-study-btn");
        tabResultsButton = root.Q<Button>("tab-results-btn");
        tabAdminButton = root.Q<Button>("tab-admin-btn");
        errorOkButton = root.Q<Button>("error-ok-btn");

        passwordField.isPasswordField = true;

        loginButton.clicked += OnLoginClicked;
        logoutButton.clicked += OnLogoutClicked;
        tabStudyButton.clicked += ShowStudyTab;
        tabResultsButton.clicked += ShowResultsTab;
        tabAdminButton.clicked += ShowAdminTab;
        errorOkButton.clicked += HideError;

        if (AppState.IsLoggedIn)
            ShowApp();
        else
            ShowLogin();
    }

    private void OnDisable()
    {
        if (loginButton != null) loginButton.clicked -= OnLoginClicked;
        if (logoutButton != null) logoutButton.clicked -= OnLogoutClicked;
        if (tabStudyButton != null) tabStudyButton.clicked -= ShowStudyTab;
        if (tabResultsButton != null) tabResultsButton.clicked -= ShowResultsTab;
        if (tabAdminButton != null) tabAdminButton.clicked -= ShowAdminTab;
        if (errorOkButton != null) errorOkButton.clicked -= HideError;
    }

    private void OnLoginClicked()
    {
        if (database == null)
        {
            ShowError("DatabaseManager не найден в сцене.");
            return;
        }

        string login = loginField.value.Trim();
        string password = passwordField.value;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            ShowError("Введите логин и пароль.");
            return;
        }

        string error;
        UserRecord user = database.Login(login, password, out error);

        if (!string.IsNullOrEmpty(error))
        {
            ShowError("Ошибка подключения к БД:\n" + error);
            return;
        }

        if (user == null)
        {
            ShowError("Неверный логин или пароль.");
            return;
        }

        AppState.UserId = user.UserId;
        AppState.Username = user.Username;
        AppState.FullName = user.FullName;
        AppState.Role = user.Role;

        ShowApp();
    }

    private void OnLogoutClicked()
    {
        AppState.ClearUser();
        loginField.value = "";
        passwordField.value = "";
        ShowLogin();
    }

    private void ShowLogin()
    {
        loginPanel.RemoveFromClassList("hidden");
        appPanel.AddToClassList("hidden");
        errorOverlay.AddToClassList("hidden");
    }

    private void ShowApp()
    {
        loginPanel.AddToClassList("hidden");
        appPanel.RemoveFromClassList("hidden");

        string roleText = AppState.IsAdmin ? "администратор" : "студент";
        profileName.text = "Профиль: " + AppState.FullName + " (" + roleText + ")";

        if (AppState.IsAdmin)
        {
            tabAdminButton.RemoveFromClassList("hidden");
            tabResultsButton.AddToClassList("hidden");
        }
        else
        {
            tabResultsButton.RemoveFromClassList("hidden");
            tabAdminButton.AddToClassList("hidden");
        }

        FillScenarioList();
        ShowStudyTab();
    }

    private void FillScenarioList()
    {
        tasksList.Clear();

        string error;
        List<ScenarioRecord> scenarios = database.GetScenarios(out error);

        if (!string.IsNullOrEmpty(error))
        {
            tasksList.Add(MakeEmptyLabel("Не удалось загрузить сценарии: " + error));
            return;
        }

        if (scenarios.Count == 0)
        {
            tasksList.Add(MakeEmptyLabel("В таблице scenarios нет заданий."));
            return;
        }

        for (int i = 0; i < scenarios.Count; i++)
        {
            ScenarioRecord scenario = scenarios[i];

            VisualElement card = new VisualElement();
            card.AddToClassList("taskCard");

            VisualElement row = new VisualElement();
            row.AddToClassList("taskTopRow");

            VisualElement info = new VisualElement();
            info.AddToClassList("taskInfo");

            Label title = new Label(scenario.Title + " [" + scenario.Code + "]");
            title.AddToClassList("taskTitle");

            Label desc = new Label(scenario.Description);
            desc.AddToClassList("taskDesc");

            Button launch = new Button();
            launch.text = "Начать";
            launch.AddToClassList("taskLaunchBtn");
            launch.clicked += delegate { LaunchScenario(scenario); };

            info.Add(title);
            info.Add(desc);
            row.Add(info);
            row.Add(launch);
            card.Add(row);
            tasksList.Add(card);
        }
    }

    private void LaunchScenario(ScenarioRecord scenario)
    {
        AppState.CurrentScenarioCode = scenario.Code;
        AppState.CurrentScenarioTitle = scenario.Title;

        string sceneName = GetSceneNameForScenario(scenario.Code);
        SceneManager.LoadScene(sceneName);
    }

    private string GetSceneNameForScenario(string scenarioCode)
    {
        if (scenarioScenes != null)
        {
            for (int i = 0; i < scenarioScenes.Length; i++)
            {
                if (string.Equals(scenarioScenes[i].scenarioCode, scenarioCode, StringComparison.OrdinalIgnoreCase))
                    return scenarioScenes[i].sceneName;
            }
        }

        return defaultScenarioScene;
    }

    private void ShowStudyTab()
    {
        ShowOnly(studyView);
        SetActiveTab(tabStudyButton);
    }

    private void ShowResultsTab()
    {
        if (AppState.IsAdmin)
            return;

        ShowOnly(resultsView);
        SetActiveTab(tabResultsButton);
        FillStudentAttempts();
    }

    private void ShowAdminTab()
    {
        if (!AppState.IsAdmin)
            return;

        ShowOnly(adminView);
        SetActiveTab(tabAdminButton);
        FillAdminAttempts();
    }

    private void ShowOnly(VisualElement view)
    {
        studyView.AddToClassList("hidden");
        resultsView.AddToClassList("hidden");
        adminView.AddToClassList("hidden");
        view.RemoveFromClassList("hidden");
    }

    private void SetActiveTab(Button activeButton)
    {
        tabStudyButton.RemoveFromClassList("tabButtonActive");
        tabResultsButton.RemoveFromClassList("tabButtonActive");
        tabAdminButton.RemoveFromClassList("tabButtonActive");
        activeButton.AddToClassList("tabButtonActive");
    }

    private void FillStudentAttempts()
    {
        resultsAttemptsScroll.Clear();

        string error;
        List<AttemptRecord> attempts = database.GetStudentAttempts(AppState.UserId, out error);

        if (!string.IsNullOrEmpty(error))
        {
            resultsAttemptsScroll.Add(MakeEmptyLabel("Не удалось загрузить результаты: " + error));
            return;
        }

        if (attempts.Count == 0)
        {
            resultsAttemptsScroll.Add(MakeEmptyLabel("Пока нет попыток."));
            return;
        }

        for (int i = 0; i < attempts.Count; i++)
        {
            AttemptRecord a = attempts[i];
            VisualElement row = MakeRow();
            row.Add(MakeCell(a.ScenarioTitle, "colScenario"));
            row.Add(MakeCell(a.CompletedAt.ToString("dd.MM.yyyy HH:mm"), "colDate"));
            row.Add(MakeCell(FormatDuration(a.DurationSeconds), "colDuration"));
            row.Add(MakeCell("—", "colErrors"));
            row.Add(MakeGradeCell(a.Grade));
            resultsAttemptsScroll.Add(row);
        }
    }

    private void FillAdminAttempts()
    {
        adminAttemptsScroll.Clear();

        string error;
        List<AttemptRecord> attempts = database.GetAllAttempts(out error);

        if (!string.IsNullOrEmpty(error))
        {
            adminAttemptsScroll.Add(MakeEmptyLabel("Не удалось загрузить результаты: " + error));
            return;
        }

        if (attempts.Count == 0)
        {
            adminAttemptsScroll.Add(MakeEmptyLabel("Пока нет попыток студентов."));
            return;
        }

        for (int i = 0; i < attempts.Count; i++)
        {
            AttemptRecord a = attempts[i];
            VisualElement row = MakeRow();
            row.Add(MakeCell(a.StudentName, "colStudent"));
            row.Add(MakeCell(a.GroupName, "colGroup"));
            row.Add(MakeCell(a.ScenarioTitle, "colScenario"));
            row.Add(MakeCell(a.CompletedAt.ToString("dd.MM.yyyy HH:mm"), "colDate"));
            row.Add(MakeCell(FormatDuration(a.DurationSeconds), "colDuration"));
            row.Add(MakeCell("—", "colErrors"));
            row.Add(MakeGradeCell(a.Grade));
            adminAttemptsScroll.Add(row);
        }
    }

    private VisualElement MakeRow()
    {
        VisualElement row = new VisualElement();
        row.AddToClassList("tableDataRow");
        return row;
    }

    private Label MakeCell(string text, string columnClass)
    {
        Label label = new Label(text);
        label.AddToClassList("tableCell");
        label.AddToClassList(columnClass);
        return label;
    }

    private VisualElement MakeGradeCell(int grade)
    {
        VisualElement cell = new VisualElement();
        cell.AddToClassList("tableCell");
        cell.AddToClassList("colGrade");

        Label pill = new Label(grade.ToString());
        pill.AddToClassList("gradePill");
        cell.Add(pill);
        return cell;
    }

    private Label MakeEmptyLabel(string text)
    {
        Label label = new Label(text);
        label.AddToClassList("emptyState");
        return label;
    }

    private void ShowError(string text)
    {
        errorMessage.text = text;
        errorOverlay.RemoveFromClassList("hidden");
    }

    private void HideError()
    {
        errorOverlay.AddToClassList("hidden");
    }

    private string FormatDuration(int seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);

        if (time.TotalHours >= 1)
            return string.Format("{0:00}:{1:00}:{2:00}", (int)time.TotalHours, time.Minutes, time.Seconds);

        return string.Format("{0:00}:{1:00}", time.Minutes, time.Seconds);
    }
}

[Serializable]
public class ScenarioSceneLink
{
    public string scenarioCode;
    public string sceneName;
}
