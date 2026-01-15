# Media Ratings Platform (MRP) - Development Protocol

## Technical Steps and Architecture Decisions
The application is built as a standalone RESTful HTTP server using C#. The following architectural decisions were made to ensure scalability and maintainability:

### 1. Layered Architecture (Controller-Repository Pattern)
The project follows a separation of concerns by dividing logic into three distinct layers:
* **Controllers**: Handle incoming `HttpListenerContext` requests, manage authentication checks, and send JSON responses.
* **Repositories**: Contain all PostgreSQL-specific logic and SQL queries. This abstracts the data access away from the HTTP logic.
* **Models**: Simple Data Transfer Objects (DTOs) representing entities like `User`, `MediaEntry`, and `Rating`.

### 2. HTTP Protocol Stack
As per the requirements, no high-level frameworks like ASP.NET were used. Instead:
* **HttpListener**: Used to implement the low-level HTTP server.
* **Regex Routing**: A custom `Router` class uses Regular Expressions to map URL paths (including dynamic IDs) to specific controller actions.
* **Newtonsoft.Json**: Employed for object serialization and deserialization.

### 3. Data Persistence
Data is persisted in a **PostgreSQL** database. To maintain referential integrity, we utilized foreign keys with `ON DELETE CASCADE` actions for ratings and favorites.

---

## Unit Test Coverage and Logic Validation
A suite of 20 integration/unit tests was implemented to validate core business logic.

### Why Specific Logic Was Tested
* **Authentication**: Tests verify that passwords are never stored in plain text but as **BCrypt** hashes, and that tokens accurately identify users.
* **Database Constraints**: Tests ensure that the "one rating per user per media" rule is enforced at the database level.
* **Calculation Logic**: Verified that the average score and rating count for media entries are calculated correctly during SQL joins.
* **Recommendation Engine**: Specific tests ensure that content similarity (matching genre, type, and age restriction) returns expected results.
* **Security**: Search filters were tested against SQL injection attempts to ensure parameterization is working correctly.

---

## Problems Encountered and Solutions

### 1. Complexity of REST Routing
* **Problem**: Implementing RESTful paths like `/api/media/{id}/rate` without a framework proved difficult for standard string splitting.
* **Solution**: Implemented a **Regex-based router** that identifies segments and extracts integer IDs from the URL path reliably.

### 2. Moderation Visibility
* **Problem**: Ensuring comments are not public until confirmed while still allowing the author to see their "draft" rating.
* **Solution**: Added an `is_confirmed` boolean to the `ratings` table. The `GetRatingsForMedia` query explicitly filters for `is_confirmed = true`.

### 3. Password Security
* **Problem**: Storing plain text passwords is a security risk.
* **Solution**: Integrated the **BCrypt.Net** library to handle high-entropy hashing and verification during login.

---

## Estimated Time Tracking

| Major Task | Estimated Hours |
| :--- | :--- |
| Initial Setup & `HttpListener` Server | 4 Hours |
| Database Schema Design & PostgreSQL Setup | 3 Hours |
| User Auth & Token Management Logic | 5 Hours |
| Media Management (CRUD) & Search Filters | 6 Hours |
| Rating System & Moderation Logic | 6 Hours |
| Recommendation Engine (SQL Aggregates) | 5 Hours |
| Integration Testing & Postman Collection | 6 Hours |
| **Total** | **35 Hours** |

---

## Architecture Design
The architecture follows a clean separation of concerns, ensuring that the HTTP layer (Controllers) is decoupled from the Data Access layer (Repositories).



## GitHub Repository
* [Link to GitHub Repository](https://github.com/mfatihy70/MediaRatingsPlatform.git)