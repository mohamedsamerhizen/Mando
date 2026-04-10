# Mando API

**Mando API** is a production-style ASP.NET Core Web API for managing field sales operations end-to-end — including authentication, customers, visits, orders, payments, notifications, reporting, operational dashboards, and auditability.

This project was built to reflect **real backend engineering**, not tutorial-level CRUD.  
It focuses on **workflow correctness, clean service boundaries, role-based authorization, operational visibility, and production-minded design**.

---

## What This Project Solves

Mando API models the lifecycle of a real field sales platform:

- Sales reps authenticate and operate within role-based permissions
- Customers are managed with business-aware workflows
- Visits are tracked as operational events, not just records
- Orders are created through valid process boundaries
- Payments go through controlled submission and review flows
- Managers gain visibility through dashboards, reports, alerts, and audit trails

This is the kind of backend system where **business rules, consistency, and traceability** matter as much as data persistence.

---

## Core Capabilities

### Authentication & Authorization
- JWT-based authentication
- ASP.NET Core Identity integration
- Role-based authorization
- Current-user resolution
- Safer user-state validation for protected flows

### Customer Management
- Customer creation and updates
- Sales-rep assignment tracking
- Customer status and operational context
- Customer financial/reporting support

### Visit Workflow
- Visit lifecycle handling
- Operational tracking of field activity
- Media/image support for visit-related flows
- Visit history and traceability

### Order Management
- Order creation within valid business workflows
- Order lifecycle control
- Reporting-ready operational data

### Payment Workflow
- Payment submission
- Approval / rejection flow
- Duplicate reference protection
- Concurrency-aware handling for sensitive financial operations

### Notifications & Operations
- User notifications
- Unread summaries
- Operational visibility
- Dashboard-oriented monitoring support

### Reporting & Auditability
- Sales and collections reporting
- Operational dashboards
- Performance-oriented summaries
- Audit logs for sensitive or important actions

---

## Engineering Focus

This repository was intentionally built around the qualities that matter in serious backend systems:

- **Thin controllers**
- **Service-driven business logic**
- **Clear separation of concerns**
- **Consistent API contracts**
- **Workflow-oriented design**
- **EF Core discipline**
- **Production-aware startup behavior**
- **Integration-testable architecture**
- **Operational realism**
- **Reviewer-friendly code organization**

---

## Architecture Overview

The solution is organized to keep HTTP concerns, business rules, and persistence responsibilities clearly separated:

```text
Mando.sln
├── Mando.Api
│   ├── Controllers
│   ├── Services
│   ├── Interfaces
│   ├── DTOs
│   ├── Entities
│   ├── Data
│   ├── Configurations
│   ├── Middleware
│   ├── Helpers
│   ├── Extensions
│   └── Common
└── Mando.Api.IntegrationTests
    ├── Auth
    ├── Contracts
    └── Infrastructure