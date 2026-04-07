import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Package, ChevronRight, Clock, Truck, CheckCircle, XCircle } from 'lucide-react';
import api from '../api';

const statusConfig = {
  pending: { color: 'bg-yellow-100 text-yellow-700', icon: Clock, label: 'Pending' },
  confirmed: { color: 'bg-blue-100 text-blue-700', icon: CheckCircle, label: 'Confirmed' },
  processing: { color: 'bg-indigo-100 text-indigo-700', icon: Package, label: 'Processing' },
  shipped: { color: 'bg-purple-100 text-purple-700', icon: Truck, label: 'Shipped' },
  delivered: { color: 'bg-green-100 text-green-700', icon: CheckCircle, label: 'Delivered' },
  cancelled: { color: 'bg-red-100 text-red-700', icon: XCircle, label: 'Cancelled' },
};

export default function Orders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get('/orders')
      .then(res => setOrders(res.data))
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

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <h1 className="text-3xl font-bold text-gray-800 mb-8">My Orders</h1>

      {orders.length === 0 ? (
        <div className="text-center py-20">
          <Package className="w-16 h-16 text-gray-300 mx-auto mb-4" />
          <h2 className="text-xl font-bold text-gray-800 mb-2">No orders yet</h2>
          <p className="text-gray-500 mb-6">Start shopping to place your first order!</p>
          <Link to="/products" className="btn-primary">Browse Products</Link>
        </div>
      ) : (
        <div className="space-y-4">
          {orders.map((order) => {
            const status = statusConfig[order.status] || statusConfig.pending;
            const StatusIcon = status.icon;
            return (
              <Link key={order.id} to={`/orders/${order.id}`} className="card p-4 sm:p-6 flex flex-col sm:flex-row sm:items-center gap-4 group">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-2">
                    <span className="font-bold text-gray-800">Order #{order.id}</span>
                    <span className={`badge ${status.color}`}>
                      <StatusIcon className="w-3 h-3 mr-1" /> {status.label}
                    </span>
                  </div>
                  <div className="text-sm text-gray-500">
                    {new Date(order.created_at).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}
                    {' · '}
                    {order.items?.length || 0} item{(order.items?.length || 0) !== 1 ? 's' : ''}
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <span className="text-xl font-bold text-primary-700">${order.total.toFixed(2)}</span>
                  <ChevronRight className="w-5 h-5 text-gray-400 group-hover:text-primary-600 transition-colors" />
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}
