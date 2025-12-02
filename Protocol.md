# MRP Project Protocol

**Student Name:** Muhammet fatih Yildiz
**Git Repository:** [PASTE YOUR GITHUB LINK HERE]

## 1. Architecture Decisions
I chose a **Layered Architecture** to adhere to **SOLID principles** (specifically Separation of Concerns):

* **Models:** Plain C# objects (POCOs) representing the database tables.
* **Data Layer (Repositories):** Handles all SQL logic. This separates database specifics from the HTTP logic.
* **Controllers:** Handles HTTP requests, deserialization, and authentication checks. They do not contain SQL.
* **HttpServer & Router:** A custom implementation using `HttpListener` to avoid using ASP.NET/Spring (as per requirements).

**Design Pattern Used:**
* **Repository Pattern:** To abstract the data access.
* **Dependency Injection (Manual):** The `Program.cs` injects the `Database` into Repositories, and Repositories into Controllers.

## 2. Technical Steps
1.  **Database:** Set up PostgreSQL with tables for `users` and `media`.
2.  **Server:** Implemented an async loop using `HttpListener`.
3.  **Routing:** Built a custom string-matching router to handle `GET`, `POST`, `PUT`, `DELETE`.
4.  **Auth:** Implemented a simple Token-based auth (Bearer Token) stored in the database.

## 3. Unit Testing Strategy
* *Note: Detailed unit tests are planned for the final submission.*
* Current testing was done using **Postman**.
* The logic was designed with dependency injection to allow mocking repositories in the future.

## 4. Problems & Solutions
* **Problem:** JSON Case Sensitivity. The Spec requires camelCase (`username`), but C# uses PascalCase (`Username`).
    * **Solution:** Used Newtonsoft.Json settings or accepted that standard deserialization handles case-insensitivity reading.
* **Problem:** Handling synchronous PostgreSQL calls in an async HTTP server.
    * **Solution:** Returned `Task.CompletedTask` in controllers to satisfy the async interface while keeping DB calls simple for now.

## 5. Time Tracking
* Database Setup: 1h
* HTTP Server Skeleton: 2h
* User/Auth Implementation: 2h
* Media CRUD Implementation: 2h
* Refactoring & Testing: 1h
* **Total:** ~8 hours

**Note:** The git history will probably be only a few commits due to time constraints.