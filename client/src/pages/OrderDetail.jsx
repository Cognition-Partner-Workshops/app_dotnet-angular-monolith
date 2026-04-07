import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { ArrowLeft, Package, Truck, CreditCard, MapPin, Phone, User } from 'lucide-react';
import api from '../api';
import toast from 'react-hot-toast';

const statusSteps = ['pending', 'confirmed', 'processing', 'shipped', 'delivered'];

export default function OrderDetail() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.get(`/orders/${id}`)
      .then(res => setOrder(res.data))
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [id]);

  const handleCancel = async () => {
    if (!confirm('Are you sure you want to cancel this order?')) return;
    try {
      await api.put(`/orders/${id}/cancel`);
      setOrder({ ...order, status: 'cancelled' });
      toast.success('Order cancelled');
    } catch (err) {
      toast.error(err.response?.data?.error || 'Failed to cancel');
    }
  };

  if (loading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="animate-spin rounded-full h-10 w-10 border-4 border-primary-600 border-t-transparent"></div>
      </div>
    );
  }

  if (!order) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-20 text-center">
        <p className="text-gray-500 text-lg">Order not found</p>
        <Link to="/orders" className="btn-primary mt-4 inline-block">View Orders</Link>
      </div>
    );
  }

  const currentStep = statusSteps.indexOf(order.status);

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <Link to="/orders" className="inline-flex items-center gap-1 text-gray-500 hover:text-primary-600 mb-6 text-sm font-medium">
        <ArrowLeft className="w-4 h-4" /> Back to Orders
      </Link>

      <div className="flex flex-col sm:flex-row sm:items-center justify-between mb-8 gap-4">
        <div>
          <h1 className="text-3xl font-bold text-gray-800">Order #{order.id}</h1>
          <p className="text-gray-500 mt-1">
            Placed on {new Date(order.created_at).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
          </p>
        </div>
        {order.status === 'pending' && (
          <button onClick={handleCancel} className="btn-danger">Cancel Order</button>
        )}
      </div>

      {/* Status Tracker */}
      {order.status !== 'cancelled' && (
        <div className="card p-6 mb-6">
          <h2 className="font-bold text-gray-800 mb-4">Order Status</h2>
          <div className="flex items-center justify-between">
            {statusSteps.map((step, idx) => (
              <div key={step} className="flex items-center flex-1 last:flex-none">
                <div className={`flex flex-col items-center ${idx <= currentStep ? 'text-primary-600' : 'text-gray-300'}`}>
                  <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold ${
                    idx <= currentStep ? 'bg-primary-600 text-white' : 'bg-gray-200 text-gray-400'
                  }`}>
                    {idx + 1}
                  </div>
                  <span className="text-xs mt-1 capitalize hidden sm:block">{step}</span>
                </div>
                {idx < statusSteps.length - 1 && (
                  <div className={`flex-1 h-1 mx-2 rounded ${idx < currentStep ? 'bg-primary-600' : 'bg-gray-200'}`} />
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {order.status === 'cancelled' && (
        <div className="card p-6 mb-6 bg-red-50 border-red-200">
          <p className="text-red-700 font-medium">This order has been cancelled.</p>
        </div>
      )}

      <div className="grid md:grid-cols-2 gap-6 mb-6">
        {/* Shipping */}
        <div className="card p-6">
          <div className="flex items-center gap-2 mb-3">
            <Truck className="w-5 h-5 text-primary-600" />
            <h2 className="font-bold text-gray-800">Shipping Details</h2>
          </div>
          <div className="space-y-2 text-sm">
            <div className="flex items-center gap-2"><User className="w-4 h-4 text-gray-400" /> {order.shipping_name}</div>
            <div className="flex items-center gap-2"><MapPin className="w-4 h-4 text-gray-400" /> {order.shipping_address}, {order.shipping_city}, {order.shipping_state} {order.shipping_zip}</div>
            <div className="flex items-center gap-2"><Phone className="w-4 h-4 text-gray-400" /> {order.shipping_phone}</div>
          </div>
        </div>

        {/* Payment */}
        <div className="card p-6">
          <div className="flex items-center gap-2 mb-3">
            <CreditCard className="w-5 h-5 text-primary-600" />
            <h2 className="font-bold text-gray-800">Payment Info</h2>
          </div>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-500">Method</span>
              <span className="font-medium capitalize">{order.payment_method === 'cod' ? 'Cash on Delivery' : order.payment_method}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-500">Payment Status</span>
              <span className={`badge ${order.payment_status === 'paid' ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'}`}>
                {order.payment_status}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Order Items */}
      <div className="card p-6">
        <div className="flex items-center gap-2 mb-4">
          <Package className="w-5 h-5 text-primary-600" />
          <h2 className="font-bold text-gray-800">Order Items</h2>
        </div>
        <div className="divide-y divide-gray-100">
          {order.items?.map((item) => (
            <div key={item.id} className="flex items-center justify-between py-3">
              <div>
                <p className="font-medium text-gray-800">{item.product_name}</p>
                <p className="text-sm text-gray-500">Qty: {item.quantity} x ${item.price.toFixed(2)}</p>
              </div>
              <span className="font-bold text-gray-800">${(item.quantity * item.price).toFixed(2)}</span>
            </div>
          ))}
        </div>
        <div className="border-t border-gray-200 mt-3 pt-3 flex justify-between">
          <span className="font-bold text-gray-800 text-lg">Total</span>
          <span className="font-bold text-xl text-primary-700">${order.total.toFixed(2)}</span>
        </div>
      </div>
    </div>
  );
}
