### Distributed Event Ticketing Platform

A high-performance, secure, and scalable event management and ticketing platform designed to handle high-concurrency ticket sales, automated vendor payouts, and distributed background processing. 

### ⚠️ Intellectual Property & Usage Notice

**This repository is Source-Available for architectural review and portfolio evaluation purposes only.** 

* **Permitted:** Reviewing the code structure, design patterns, and engineering choices via the GitHub web interface.
* **Prohibited:** Cloning, downloading, local execution, or duplicating any part of this codebase.
* Please refer to the [Copyright & Licensing](#-copyright--licensing) section at the bottom of this document for full legal restrictions.

### 🏗️ System Architecture & Tech Stack

The application is built using a modern, decoupled architecture split into a high-concurrency API layer, an asynchronous worker service, and an optimized frontend. 

### Frontend (Single Page Application)

* **React (TypeScript):** Type-safe component architecture with optimized state management for smooth user flows.
* **Stripe Embedded Checkout:** Seamless, secure UI integration for ticket purchases minimizing PCI compliance overhead.

### Backend Services (Containerized REST API & Workers)

* **.NET Core Web API:** High-throughput, clean architecture API handling business logic, validation, and core endpoints.
* **.NET Core Background Worker System:** Dedicated background service handling asynchronous processes (e.g., event reminder emails, database cleanups, and expiring uncompleted ticket reservations).

### Data & Caching Layer

* **MySQL (AWS RDS):** Relational database optimized with indexing for transactional consistency during concurrent ticket orders.
* **Redis:** In-memory data store utilized for ultra-fast session state management and caching hot event metadata.

### Infrastructure & Deployment (AWS & Docker)

* **Docker Compose:** Multi-container orchestration grouping the .NET API, Background Workers, and local Redis into unified, repeatable deployment environments.
* **AWS EC2 (Linux):** Hosts the containerized backend services.
* **AWS CloudFront & S3:** Globally distributed CDN serving the optimized React SPA with low latency.
* **AWS RDS:** Managed, highly available database layer running MySQL.

### 🌟 Key Engineering Highlights for Reviewers

When auditing this codebase, please take note of the following production-ready implementations: 

### 1. Multi-Tenant Stripe Connect Integration

Unlike standard payment gateways, this platform implements **Stripe Connect**. Transactions are executed securely **on behalf of the event organizers**. The system dynamically routes payments, handles direct platform fees, and automates vendor payouts while keeping financial data completely isolated and compliant. 

### 2. High-Concurrency Asynchronous Workers

To keep the main API snappy and highly responsive, intensive operations are offloaded entirely to the **.NET Background Worker**. Check out the worker implementation to see how it manages thread-safe scheduling for automated event reminders and critical database maintenance tasks without degrading user checkout performance. 

### 3. Containerized Micro-Environment

The backend infrastructure is completely localized using **Docker Compose**. This ensures that the configuration, network isolation between the .NET API and the Redis caching box, and environment dependencies mirror production accurately right out of the box. 

### 🔐 Copyright & Licensing

**Copyright (c) 2026. All rights reserved.** 

This software, including all source code, documentation, configuration files, and visual assets, is proprietary and confidential. 

1. **No License Granted:** Except as explicitly stated below, no license, right, or title to this software is transferred to any person or entity.
2. **Limited View-Only Exception:** Permission is granted solely to view the source code within this GitHub repository through a standard web browser for the purposes of employment evaluation, technical auditing, or educational review.
3. **Strict Restrictions:** You may not download, clone, copy, modify, distribute, reverse engineer, or create derivative works of this software for any reason, commercial or non-commercial, without explicit written permission from the copyright holder.

*Note: For a live demonstration of the platform or to discuss the system design in further detail, please reach out directly via LinkedIn or email.*