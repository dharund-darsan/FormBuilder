# 🧾 FormsBuilder – Google Forms Replica

![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet?style=flat-square)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)
![Database](https://img.shields.io/badge/Database-SQL--Server-blue?style=flat-square)
![Status](https://img.shields.io/badge/Status-In%20Development-orange?style=flat-square)

> A lightweight and modern **Google Forms clone** built with **ASP.NET Core** and **SQL** — allowing users to create, share, and analyze forms and surveys easily.

---

## 🚀 Overview

**FormsBuilder** is a full-stack web application inspired by **Google Forms**.  
It allows users to create customizable forms with different question types, collect responses securely, and view analytics — all through a clean and responsive interface.

This project is ideal for learning **modern web development concepts** such as:
- RESTful APIs  
- Authentication using JWT & Cookies  
- Relational database modeling  
- Unit testing with xUnit  
- Secure and scalable backend development with **.NET 8**

---

## ✨ Features

### 🔐 Authentication & Users
- Secure user registration and login  
- JWT authentication stored in **HTTP-only cookies**  
- Role-based access (Admin / Creator / Responder)

### 🧩 Form Creation
- Create and edit forms dynamically  
- Add multiple question types:
  - Short answer  
  - Paragraph  
  - Multiple choice  
  - Checkboxes  
  - Dropdown
- Reorder, duplicate, or delete questions  
- Add form titles and descriptions  

### 📤 Form Sharing
- Public and private form access  
- Share forms via unique link or code  

### 🧾 Form Responses
- Submit responses without login (optional)  
- Real-time response collection  
- Prevent duplicate submissions  

### 📊 Response Analysis
- View detailed and summary responses  
- Visual charts for multiple-choice statistics  
- Export responses as CSV  

---

## 🧠 Minor / Nice-to-Have Features
- ✅ Auto-save while editing forms  
- 🌙 Dark mode support  
- 📈 Response charts using Chart.js or Recharts  
- 🔄 Form duplication  
- 📬 Email notifications for new responses  
- 🧰 Basic admin dashboard  

---

## 🛠️ Tech Stack

| Layer | Technology |
|-------|-------------|
| **Frontend** | React.js / Next.js (or Razor Pages) |
| **Backend** | ASP.NET Core 8 (Web API) |
| **Database** | SQL Server / PostgreSQL (via Dapper or EF Core) |
| **Authentication** | JWT + HTTP-only Cookies |
| **Testing** | xUnit, Moq |
| **Styling** | Tailwind CSS |
| **Version Control** | Git + GitHub |

---

## 🧱 Project Structure

