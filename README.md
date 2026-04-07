# DairyFresh - Fresh Dairy Products E-Commerce

A full-stack, mobile-friendly web application for selling fresh dairy products online.

## Features

- **Product Catalog** - Browse dairy products with categories, search, and sorting
- **Shopping Cart** - Add/remove items, update quantities, real-time totals
- **User Authentication** - JWT-based registration and login
- **Checkout & Orders** - Complete checkout flow with order tracking
- **Payment Integration** - Multiple payment methods (COD, Card, UPI - demo mode)
- **Admin Dashboard** - Manage products, orders, customers, and view analytics
- **Mobile-Friendly** - Responsive design optimized for all screen sizes

## Tech Stack

- **Frontend:** React 18 + Vite + Tailwind CSS + React Router
- **Backend:** Node.js + Express.js
- **Database:** SQLite (via better-sqlite3)
- **Auth:** JWT (jsonwebtoken + bcryptjs)
- **Icons:** Lucide React

## Quick Start

```bash
# Install dependencies
npm install
cd client && npm install && cd ..

# Seed the database with sample data
npm run seed

# Start development server (both frontend & backend)
npm run dev
```

The app will be available at:
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000

## Demo Credentials

| Role     | Email                  | Password    |
|----------|------------------------|-------------|
| Admin    | admin@dairyfresh.com   | admin123    |
| Customer | john@example.com       | customer123 |

## API Endpoints

### Auth
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login
- `GET /api/auth/me` - Get current user
- `PUT /api/auth/me` - Update profile

### Products
- `GET /api/products` - List products (with filters)
- `GET /api/products/featured` - Featured products
- `GET /api/products/categories` - List categories
- `GET /api/products/:id` - Product detail

### Cart (authenticated)
- `GET /api/cart` - Get cart
- `POST /api/cart` - Add to cart
- `PUT /api/cart/:id` - Update quantity
- `DELETE /api/cart/:id` - Remove item
- `DELETE /api/cart` - Clear cart

### Orders (authenticated)
- `POST /api/orders` - Create order
- `GET /api/orders` - List user orders
- `GET /api/orders/:id` - Order detail
- `PUT /api/orders/:id/cancel` - Cancel order

### Admin (admin only)
- `GET /api/admin/stats` - Dashboard statistics
- `GET /api/admin/products` - All products
- `POST /api/admin/products` - Create product
- `PUT /api/admin/products/:id` - Update product
- `DELETE /api/admin/products/:id` - Deactivate product
- `GET /api/admin/orders` - All orders
- `PUT /api/admin/orders/:id` - Update order status
- `GET /api/admin/customers` - All customers

## Project Structure

```
dairy-fresh/
├── server/                 # Backend
│   ├── index.js           # Express server
│   ├── database.js        # SQLite setup & schema
│   ├── seed.js            # Sample data seeder
│   ├── middleware/
│   │   └── auth.js        # JWT auth middleware
│   └── routes/
│       ├── auth.js        # Auth endpoints
│       ├── products.js    # Product endpoints
│       ├── cart.js        # Cart endpoints
│       ├── orders.js      # Order endpoints
│       └── admin.js       # Admin endpoints
├── client/                 # Frontend
│   ├── src/
│   │   ├── components/    # Reusable components
│   │   ├── pages/         # Page components
│   │   ├── context/       # React context (Auth, Cart)
│   │   └── api.js         # Axios API client
│   └── ...config files
└── package.json
```
