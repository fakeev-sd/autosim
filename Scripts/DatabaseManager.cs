using System;
using System.Collections.Generic;
using Npgsql;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instance { get; private set; }

    [Header("PostgreSQL")]
    [SerializeField] private string host = "localhost";
    [SerializeField] private int port = 5432;
    [SerializeField] private string database = "mechanic_sim";
    [SerializeField] private string username = "postgres";
    [SerializeField] private string password = "postgres";

    private string ConnectionString
    {
        get
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
            builder.Host = host;
            builder.Port = port;
            builder.Database = database;
            builder.Username = username;
            builder.Password = password;
            builder.Timeout = 3;
            builder.CommandTimeout = 5;
            return builder.ConnectionString;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool CheckConnection(out string error)
    {
        error = "";

        try
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
            }
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public UserRecord Login(string login, string plainPassword, out string error)
    {
        error = "";

        try
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT user_id, username, first_name, last_name, role
                        FROM users
                        WHERE lower(username) = lower(@login)
                          AND password_hash = @password
                        LIMIT 1;";

                    command.Parameters.AddWithValue("login", login.Trim());
                    command.Parameters.AddWithValue("password", plainPassword);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        UserRecord user = new UserRecord();
                        user.UserId = reader.GetInt32(0);
                        user.Username = reader.GetString(1);
                        user.FirstName = reader.GetString(2);
                        user.LastName = reader.GetString(3);
                        user.Role = reader.GetString(4);
                        return user;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    public List<ScenarioRecord> GetScenarios(out string error)
    {
        error = "";
        List<ScenarioRecord> scenarios = new List<ScenarioRecord>();

        try
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT code, title, COALESCE(description, '')
                        FROM scenarios
                        ORDER BY scenario_id;";

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ScenarioRecord scenario = new ScenarioRecord();
                            scenario.Code = reader.GetString(0);
                            scenario.Title = reader.GetString(1);
                            scenario.Description = reader.GetString(2);
                            scenarios.Add(scenario);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        return scenarios;
    }

    public List<AttemptRecord> GetStudentAttempts(int userId, out string error)
    {
        error = "";
        List<AttemptRecord> attempts = new List<AttemptRecord>();

        try
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT s.title, a.completed_at, a.duration_seconds, a.grade
                        FROM attempts a
                        JOIN scenarios s ON s.scenario_id = a.scenario_id
                        WHERE a.user_id = @user_id
                        ORDER BY a.completed_at DESC;";

                    command.Parameters.AddWithValue("user_id", userId);

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AttemptRecord attempt = new AttemptRecord();
                            attempt.ScenarioTitle = reader.GetString(0);
                            attempt.CompletedAt = reader.GetDateTime(1);
                            attempt.DurationSeconds = reader.GetInt32(2);
                            attempt.Grade = reader.GetInt16(3);
                            attempts.Add(attempt);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        return attempts;
    }

    public List<AttemptRecord> GetAllAttempts(out string error)
    {
        error = "";
        List<AttemptRecord> attempts = new List<AttemptRecord>();

        try
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT
                            u.last_name || ' ' || u.first_name AS student_name,
                            COALESCE(g.name, '-') AS group_name,
                            s.title,
                            a.completed_at,
                            a.duration_seconds,
                            a.grade
                        FROM attempts a
                        JOIN users u ON u.user_id = a.user_id
                        LEFT JOIN groups g ON g.group_id = u.group_id
                        JOIN scenarios s ON s.scenario_id = a.scenario_id
                        WHERE u.role = 'student'
                        ORDER BY a.completed_at DESC;";

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AttemptRecord attempt = new AttemptRecord();
                            attempt.StudentName = reader.GetString(0);
                            attempt.GroupName = reader.GetString(1);
                            attempt.ScenarioTitle = reader.GetString(2);
                            attempt.CompletedAt = reader.GetDateTime(3);
                            attempt.DurationSeconds = reader.GetInt32(4);
                            attempt.Grade = reader.GetInt16(5);
                            attempts.Add(attempt);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        return attempts;
    }

    public bool SaveAttempt(int userId, string scenarioCode, int durationSeconds, int grade, out string error)
    {
        error = "";

        try
        {
            using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                int scenarioId = -1;
                using (NpgsqlCommand findCommand = connection.CreateCommand())
                {
                    findCommand.CommandText = @"
                        SELECT scenario_id
                        FROM scenarios
                        WHERE lower(code) = lower(@code)
                        LIMIT 1;";
                    findCommand.Parameters.AddWithValue("code", scenarioCode);

                    object result = findCommand.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                    {
                        error = "Сценарий с code='" + scenarioCode + "' не найден в таблице scenarios.";
                        return false;
                    }

                    scenarioId = Convert.ToInt32(result);
                }

                using (NpgsqlCommand insertCommand = connection.CreateCommand())
                {
                    insertCommand.CommandText = @"
                        INSERT INTO attempts (user_id, scenario_id, duration_seconds, grade)
                        VALUES (@user_id, @scenario_id, @duration_seconds, @grade);";

                    insertCommand.Parameters.AddWithValue("user_id", userId);
                    insertCommand.Parameters.AddWithValue("scenario_id", scenarioId);
                    insertCommand.Parameters.AddWithValue("duration_seconds", durationSeconds);
                    insertCommand.Parameters.AddWithValue("grade", grade);
                    insertCommand.ExecuteNonQuery();
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

public class UserRecord
{
    public int UserId;
    public string Username;
    public string FirstName;
    public string LastName;
    public string Role;

    public string FullName
    {
        get { return LastName + " " + FirstName; }
    }
}

public class ScenarioRecord
{
    public string Code;
    public string Title;
    public string Description;
}

public class AttemptRecord
{
    public string StudentName;
    public string GroupName;
    public string ScenarioTitle;
    public DateTime CompletedAt;
    public int DurationSeconds;
    public int Grade;
}
