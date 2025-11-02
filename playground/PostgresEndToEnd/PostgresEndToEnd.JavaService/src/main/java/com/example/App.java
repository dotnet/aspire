package com.example;

import java.sql.*;
import java.util.*;
import java.util.UUID;
import spark.Spark;

public class App {
    public static void main(String[] args) {
        Spark.port(Integer.parseInt(System.getenv().getOrDefault("PORT", "4567")));
        Spark.get("/", (req, res) -> {
            String uri = System.getenv("JDBCCONNECTIONSTRING");
            String user = System.getenv("DB1_USERNAME");
            String password = System.getenv("DB1_PASSWORD");
            List<String> entries = new ArrayList<>();
            try (Connection conn = DriverManager.getConnection(uri, user, password)) {
                try (Statement stmt = conn.createStatement()) {
                    stmt.execute("CREATE TABLE IF NOT EXISTS entries (id UUID PRIMARY KEY);");
                }
                try (PreparedStatement ps = conn.prepareStatement("INSERT INTO entries (id) VALUES (?);");) {
                    ps.setObject(1, UUID.randomUUID());
                    ps.executeUpdate();
                }
                try (Statement stmt = conn.createStatement();
                     ResultSet rs = stmt.executeQuery("SELECT id FROM entries;")) {
                    while (rs.next()) entries.add(rs.getString("id"));
                }
            }
            res.type("application/json");
            return String.format("{\"totalEntries\": %d, \"entries\": %s}", entries.size(), entries.toString());
        });
    }
}
