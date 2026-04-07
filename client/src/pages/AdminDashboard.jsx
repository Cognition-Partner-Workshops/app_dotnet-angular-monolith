import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Package, DollarSign, Users, ShoppingBag, AlertTriangle, Clock, ArrowRight } from 'lucide-react';
import api from '../api';

export default function AdminDashboard() {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/admin/stats')
      .then(res => setStats(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="animate-spin rounded-full h-10 w-10 border-4 border-primary-600 border-t-transparent"></div>
      </div>
    );
  }

  const statCards = [
    { label: 'Total Revenue', value: `$${stats?.totalRevenue?.toFixed(2) || '0.00'}`, icon: DollarSign, color: 'bg-green-50 text-green-600' },
    { label: 'Total Orders', value: stats?.totalOrders || 0, icon: ShoppingBag, color: 'bg-blue-50 text-blue-600' },
    { label: 'Total Products', value: stats?.totalProducts || 0, icon: Package, color: 'bg-purple-50 text-purple-600' },
    { label: 'Customers', value: stats?.totalCustomers || 0, icon: Users, color: 'bg-orange-50 text-orange-600' },
    { label: 'Pending Orders', value: stats?.pendingOrders || 0, icon: Clock, color: 'bg-yellow-50 text-yellow-600' },
    { label: 'Low Stock Items', value: stats?.lowStockProducts || 0, icon: AlertTriangle, color: 'bg-red-50 text-red-600' },
  ];

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold text-gray-800">Admin Dashboard</h1>
          <p className="text-gray-500 mt-1">Overview of your dairy business</p>
        </div>
        <div className="flex gap-3">
          <Link to="/admin/products" className="btn-secondary text-sm">Manage Products</Link>
          <Link to="/admin/orders" className="btn-primary text-sm">View Orders</Link>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4 mb-8">
        {statCards.map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="card p-4">
            <div className={`w-10 h-10 rounded-lg ${color} flex items-center justify-center mb-3`}>
              <Icon className="w-5 h-5" />
            </div>
            <p className="text-2xl font-bold text-gray-800">{value}</p>
            <p className="text-xs text-gray-500 mt-1">{label}</p>
          </div>
        ))}
      </div>

      {/* Recent Orders */}
      <div className="card">
        <div className="flex items-center justify-between p-6 border-b border-gray-100">
          <h2 className="text-lg font-bold text-gray-800">Recent Orders</h2>
          <Link to="/admin/orders" className="text-sm text-primary-600 font-medium hover:text-primary-700 flex items-center gap-1">
            View All <ArrowRight className="w-4 h-4" />
          </Link>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                <th className="px-6 py-3">Order</th>
                <th className="px-6 py-3">Customer</th>
                <th className="px-6 py-3">Total</th>
                <th className="px-6 py-3">Status</th>
                <th className="px-6 py-3">Date</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {stats?.recentOrders?.map((order) => (
                <tr key={order.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 text-sm font-medium text-gray-800">#{order.id}</td>
                  <td className="px-6 py-4 text-sm text-gray-600">{order.customer_name}</td>
                  <td className="px-6 py-4 text-sm font-medium text-gray-800">${order.total.toFixed(2)}</td>
                  <td className="px-6 py-4">
                    <span className={`badge ${
                      order.status === 'delivered' ? 'bg-green-100 text-green-700' :
                      order.status === 'cancelled' ? 'bg-red-100 text-red-700' :
                      order.status === 'shipped' ? 'bg-purple-100 text-purple-700' :
                      'bg-yellow-100 text-yellow-700'
                    }`}>
                      {order.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-500">
                    {new Date(order.created_at).toLocaleDateString()}
                  </td>
                </tr>
              ))}
              {(!stats?.recentOrders || stats.recentOrders.length === 0) && (
                <tr>
                  <td colSpan="5" className="px-6 py-8 text-center text-gray-500">No orders yet</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
