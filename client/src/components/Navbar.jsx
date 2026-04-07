import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ShoppingCart, User, Menu, X, Milk, LogOut, Package, LayoutDashboard } from 'lucide-react';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';

export default function Navbar() {
  const { user, logout, isAdmin } = useAuth();
  const { cart } = useCart();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);

  const handleLogout = () => {
    logout();
    setProfileOpen(false);
    navigate('/');
  };

  return (
    <nav className="bg-white shadow-sm sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          {/* Logo */}
          <Link to="/" className="flex items-center gap-2 text-primary-700 font-bold text-xl">
            <Milk className="w-7 h-7" />
            <span className="hidden sm:inline">DairyFresh</span>
          </Link>

          {/* Desktop Nav */}
          <div className="hidden md:flex items-center gap-6">
            <Link to="/" className="text-gray-600 hover:text-primary-600 font-medium transition-colors">Home</Link>
            <Link to="/products" className="text-gray-600 hover:text-primary-600 font-medium transition-colors">Products</Link>
            {user && (
              <Link to="/orders" className="text-gray-600 hover:text-primary-600 font-medium transition-colors">My Orders</Link>
            )}
            {isAdmin && (
              <Link to="/admin" className="text-gray-600 hover:text-primary-600 font-medium transition-colors">Admin</Link>
            )}
          </div>

          {/* Right side */}
          <div className="flex items-center gap-3">
            {user && (
              <Link to="/cart" className="relative p-2 text-gray-600 hover:text-primary-600 transition-colors">
                <ShoppingCart className="w-6 h-6" />
                {cart.count > 0 && (
                  <span className="absolute -top-1 -right-1 bg-primary-600 text-white text-xs w-5 h-5 rounded-full flex items-center justify-center font-bold">
                    {cart.count}
                  </span>
                )}
              </Link>
            )}

            {user ? (
              <div className="relative">
                <button
                  onClick={() => setProfileOpen(!profileOpen)}
                  className="flex items-center gap-2 p-2 rounded-lg hover:bg-gray-100 transition-colors"
                >
                  <div className="w-8 h-8 bg-primary-100 text-primary-700 rounded-full flex items-center justify-center font-bold text-sm">
                    {user.name?.charAt(0).toUpperCase()}
                  </div>
                  <span className="hidden sm:inline text-sm font-medium text-gray-700">{user.name}</span>
                </button>

                {profileOpen && (
                  <>
                    <div className="fixed inset-0" onClick={() => setProfileOpen(false)} />
                    <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg border border-gray-100 py-1 z-50">
                      <div className="px-4 py-2 border-b border-gray-100">
                        <p className="text-sm font-medium text-gray-800">{user.name}</p>
                        <p className="text-xs text-gray-500">{user.email}</p>
                      </div>
                      <Link to="/orders" onClick={() => setProfileOpen(false)} className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
                        <Package className="w-4 h-4" /> My Orders
                      </Link>
                      {isAdmin && (
                        <Link to="/admin" onClick={() => setProfileOpen(false)} className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
                          <LayoutDashboard className="w-4 h-4" /> Admin Panel
                        </Link>
                      )}
                      <button onClick={handleLogout} className="flex items-center gap-2 w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50">
                        <LogOut className="w-4 h-4" /> Logout
                      </button>
                    </div>
                  </>
                )}
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <Link to="/login" className="text-gray-600 hover:text-primary-600 font-medium text-sm px-3 py-2">Login</Link>
                <Link to="/register" className="btn-primary text-sm !py-2 !px-4">Sign Up</Link>
              </div>
            )}

            {/* Mobile menu button */}
            <button
              onClick={() => setMobileOpen(!mobileOpen)}
              className="md:hidden p-2 text-gray-600 hover:text-primary-600"
            >
              {mobileOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
        </div>

        {/* Mobile Nav */}
        {mobileOpen && (
          <div className="md:hidden py-4 border-t border-gray-100">
            <div className="flex flex-col gap-2">
              <Link to="/" onClick={() => setMobileOpen(false)} className="px-3 py-2 text-gray-600 hover:text-primary-600 font-medium rounded-lg hover:bg-gray-50">Home</Link>
              <Link to="/products" onClick={() => setMobileOpen(false)} className="px-3 py-2 text-gray-600 hover:text-primary-600 font-medium rounded-lg hover:bg-gray-50">Products</Link>
              {user && (
                <Link to="/orders" onClick={() => setMobileOpen(false)} className="px-3 py-2 text-gray-600 hover:text-primary-600 font-medium rounded-lg hover:bg-gray-50">My Orders</Link>
              )}
              {isAdmin && (
                <Link to="/admin" onClick={() => setMobileOpen(false)} className="px-3 py-2 text-gray-600 hover:text-primary-600 font-medium rounded-lg hover:bg-gray-50">Admin Panel</Link>
              )}
            </div>
          </div>
        )}
      </div>
    </nav>
  );
}
