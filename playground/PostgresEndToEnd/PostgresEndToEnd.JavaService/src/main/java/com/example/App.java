package com.example;

import java.sql.*;
import java.util.*;
import java.util.UUID;
import spark.Spark;

public class App {
    public static void main(String[] args) {
        int port = Integer.parseInt(System.getenv().getOrDefault("PORT", "4567"));
        System.out.println("Starting Java service on port " + port);
        
        Spark.port(port);
        
        System.out.println("Configuring routes...");
        Spark.get("/", (req, res) -> {
            System.out.println("Received request to /");
            
            String uri = System.getenv("DB1_JDBCCONNECTIONSTRING");
            String user = System.getenv("DB1_USERNAME");
            String password = System.getenv("DB1_PASSWORD");
            
            System.out.println("Connecting to database: " + uri);
            List<String> entries = new ArrayList<>();
            try (Connection conn = DriverManager.getConnection(uri, user, password)) {
                System.out.println("Connected to database successfully");
                try (Statement stmt = conn.createStatement()) {
                    stmt.execute("CREATE TABLE IF NOT EXISTS entries (id UUID PRIMARY KEY);");
                    System.out.println("Table 'entries' checked/created");
                }
                try (PreparedStatement ps = conn.prepareStatement("INSERT INTO entries (id) VALUES (?);");) {
                    UUID newId = UUID.randomUUID();
                    ps.setObject(1, newId);
                    ps.executeUpdate();
                    System.out.println("Inserted new entry: " + newId);
                }
                try (Statement stmt = conn.createStatement();
                     ResultSet rs = stmt.executeQuery("SELECT id FROM entries;")) {
                    while (rs.next()) entries.add(rs.getString("id"));
                }
                System.out.println("Total entries retrieved: " + entries.size());
            } catch (Exception e) {
                System.err.println("Database error: " + e.getMessage());
                e.printStackTrace();
                throw e;
            }
            res.type("application/json");
            String response = String.format("{\"totalEntries\": %d, \"entries\": %s}", entries.size(), entries.toString());
            System.out.println("Returning response with " + entries.size() + " entries");
            return response;
        });
        
        System.out.println("Java service is ready and listening on port " + port);
    }
}
