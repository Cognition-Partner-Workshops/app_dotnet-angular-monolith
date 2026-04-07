import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, Eye, ChevronDown } from 'lucide-react';
import api from '../api';
import toast from 'react-hot-toast';

const statusOptions = ['pending', 'confirmed', 'processing', 'shipped', 'delivered', 'cancelled'];

export default function AdminOrders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('');
  const [expandedOrder, setExpandedOrder] = useState(null);

  useEffect(() => {
    setLoading(true);
    const params = filter ? `?status=${filter}` : '';
    api.get(`/admin/orders${params}`)
      .then(res => setOrders(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [filter]);

  const updateStatus = async (orderId, status) => {
    try {
      await api.put(`/admin/orders/${orderId}`, { status });
      setOrders(orders.map(o => o.id === orderId ? { ...o, status } : o));
      toast.success(`Order #${orderId} updated to ${status}`);
    } catch (err) {
      toast.error('Failed to update order');
    }
  };

  const updatePayment = async (orderId, payment_status) => {
    try {
      await api.put(`/admin/orders/${orderId}`, { payment_status });
      setOrders(orders.map(o => o.id === orderId ? { ...o, payment_status } : o));
      toast.success('Payment status updated');
    } catch (err) {
      toast.error('Failed to update payment');
    }
  };

  if (loading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="animate-spin rounded-full h-10 w-10 border-4 border-primary-600 border-t-transparent"></div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="flex items-center gap-4 mb-8">
        <Link to="/admin" className="text-gray-400 hover:text-primary-600">
          <ArrowLeft className="w-5 h-5" />
        </Link>
        <div>
          <h1 className="text-3xl font-bold text-gray-800">Manage Orders</h1>
          <p className="text-gray-500 mt-1">{orders.length} orders</p>
        </div>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-2 mb-6">
        <button onClick={() => setFilter('')} className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${!filter ? 'bg-primary-600 text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'}`}>
          All
        </button>
        {statusOptions.map(status => (
          <button key={status} onClick={() => setFilter(status)} className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors capitalize ${filter === status ? 'bg-primary-600 text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'}`}>
            {status}
          </button>
        ))}
      </div>

      {/* Orders */}
      <div className="space-y-4">
        {orders.map((order) => (
          <div key={order.id} className="card">
            <div className="p-4 sm:p-6">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-3 mb-1">
                    <span className="font-bold text-gray-800">Order #{order.id}</span>
                    <span className={`badge ${
                      order.status === 'delivered' ? 'bg-green-100 text-green-700' :
                      order.status === 'cancelled' ? 'bg-red-100 text-red-700' :
                      order.status === 'shipped' ? 'bg-purple-100 text-purple-700' :
                      'bg-yellow-100 text-yellow-700'
                    }`}>
                      {order.status}
                    </span>
                    <span className={`badge ${order.payment_status === 'paid' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                      {order.payment_status}
                    </span>
                  </div>
                  <p className="text-sm text-gray-500">
                    {order.customer_name} ({order.customer_email}) · {new Date(order.created_at).toLocaleDateString()}
                  </p>
                </div>
                <div className="flex items-center gap-3">
                  <span className="text-xl font-bold text-primary-700">${order.total.toFixed(2)}</span>
                  <div className="flex gap-2">
                    <select
                      value={order.status}
                      onChange={(e) => updateStatus(order.id, e.target.value)}
                      className="text-sm border border-gray-200 rounded-lg px-2 py-1.5 focus:ring-2 focus:ring-primary-500"
                    >
                      {statusOptions.map(s => (
                        <option key={s} value={s} className="capitalize">{s}</option>
                      ))}
                    </select>
                    <select
                      value={order.payment_status}
                      onChange={(e) => updatePayment(order.id, e.target.value)}
                      className="text-sm border border-gray-200 rounded-lg px-2 py-1.5 focus:ring-2 focus:ring-primary-500"
                    >
                      <option value="pending">Pending</option>
                      <option value="paid">Paid</option>
                      <option value="failed">Failed</option>
                      <option value="refunded">Refunded</option>
                    </select>
                    <button
                      onClick={() => setExpandedOrder(expandedOrder === order.id ? null : order.id)}
                      className="p-1.5 text-gray-400 hover:text-primary-600 transition-colors"
                    >
                      <ChevronDown className={`w-5 h-5 transition-transform ${expandedOrder === order.id ? 'rotate-180' : ''}`} />
                    </button>
                  </div>
                </div>
              </div>

              {expandedOrder === order.id && (
                <div className="mt-4 pt-4 border-t border-gray-100">
                  <div className="grid sm:grid-cols-2 gap-4 mb-4">
                    <div>
                      <p className="text-xs font-medium text-gray-500 uppercase mb-1">Shipping</p>
                      <p className="text-sm text-gray-700">{order.shipping_name}</p>
                      <p className="text-sm text-gray-500">{order.shipping_address}, {order.shipping_city}, {order.shipping_state} {order.shipping_zip}</p>
                      <p className="text-sm text-gray-500">{order.shipping_phone}</p>
                    </div>
                    <div>
                      <p className="text-xs font-medium text-gray-500 uppercase mb-1">Payment</p>
                      <p className="text-sm text-gray-700 capitalize">{order.payment_method === 'cod' ? 'Cash on Delivery' : order.payment_method}</p>
                    </div>
                  </div>
                  <div>
                    <p className="text-xs font-medium text-gray-500 uppercase mb-2">Items</p>
                    <div className="space-y-2">
                      {order.items?.map(item => (
                        <div key={item.id} className="flex justify-between text-sm">
                          <span className="text-gray-600">{item.product_name} x{item.quantity}</span>
                          <span className="font-medium">${(item.price * item.quantity).toFixed(2)}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        ))}
        {orders.length === 0 && (
          <div className="text-center py-20 text-gray-500">No orders found</div>
        )}
      </div>
    </div>
  );
}
