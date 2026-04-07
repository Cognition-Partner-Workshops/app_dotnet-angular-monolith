import { Link } from 'react-router-dom';
import { Trash2, Minus, Plus, ShoppingBag, ArrowRight } from 'lucide-react';
import { useCart } from '../context/CartContext';

export default function Cart() {
  const { cart, updateQuantity, removeFromCart, loading } = useCart();

  if (loading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="animate-spin rounded-full h-10 w-10 border-4 border-primary-600 border-t-transparent"></div>
      </div>
    );
  }

  if (cart.items.length === 0) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center">
        <ShoppingBag className="w-16 h-16 text-gray-300 mx-auto mb-4" />
        <h2 className="text-2xl font-bold text-gray-800 mb-2">Your cart is empty</h2>
        <p className="text-gray-500 mb-6">Looks like you haven't added any products yet.</p>
        <Link to="/products" className="btn-primary inline-flex items-center gap-2">
          Browse Products <ArrowRight className="w-4 h-4" />
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <h1 className="text-3xl font-bold text-gray-800 mb-8">Shopping Cart</h1>

      <div className="grid lg:grid-cols-3 gap-8">
        {/* Cart Items */}
        <div className="lg:col-span-2 space-y-4">
          {cart.items.map((item) => (
            <div key={item.id} className="card p-4 flex gap-4">
              <div className="w-20 h-20 sm:w-24 sm:h-24 rounded-lg overflow-hidden bg-gray-100 shrink-0">
                <img
                  src={item.image_url}
                  alt={item.name}
                  className="w-full h-full object-cover"
                  onError={(e) => {
                    e.target.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(item.name)}&background=dcfce7&color=166534&size=200`;
                  }}
                />
              </div>
              <div className="flex-1 min-w-0">
                <Link to={`/products/${item.product_id}`} className="font-semibold text-gray-800 hover:text-primary-600 line-clamp-1">
                  {item.name}
                </Link>
                <p className="text-sm text-gray-500 mt-0.5">${item.price.toFixed(2)} / {item.unit}</p>

                <div className="flex items-center justify-between mt-3">
                  <div className="flex items-center border border-gray-200 rounded-lg">
                    <button
                      onClick={() => updateQuantity(item.id, Math.max(1, item.quantity - 1))}
                      className="p-1.5 hover:bg-gray-50 transition-colors"
                    >
                      <Minus className="w-3.5 h-3.5" />
                    </button>
                    <span className="px-3 text-sm font-semibold">{item.quantity}</span>
                    <button
                      onClick={() => updateQuantity(item.id, Math.min(item.stock, item.quantity + 1))}
                      className="p-1.5 hover:bg-gray-50 transition-colors"
                    >
                      <Plus className="w-3.5 h-3.5" />
                    </button>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="font-bold text-primary-700">${(item.price * item.quantity).toFixed(2)}</span>
                    <button
                      onClick={() => removeFromCart(item.id)}
                      className="p-1.5 text-gray-400 hover:text-red-500 transition-colors"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Order Summary */}
        <div className="lg:col-span-1">
          <div className="card p-6 sticky top-24">
            <h3 className="text-lg font-bold text-gray-800 mb-4">Order Summary</h3>
            <div className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500">Subtotal ({cart.count} items)</span>
                <span className="font-medium">${cart.total.toFixed(2)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Delivery</span>
                <span className="font-medium text-green-600">{cart.total >= 30 ? 'Free' : '$4.99'}</span>
              </div>
              <div className="border-t border-gray-200 pt-3 flex justify-between">
                <span className="font-bold text-gray-800">Total</span>
                <span className="font-bold text-xl text-primary-700">
                  ${(cart.total + (cart.total >= 30 ? 0 : 4.99)).toFixed(2)}
                </span>
              </div>
            </div>
            {cart.total < 30 && (
              <p className="text-xs text-gray-400 mt-3">Add ${(30 - cart.total).toFixed(2)} more for free delivery</p>
            )}
            <Link to="/checkout" className="btn-primary w-full mt-6 flex items-center justify-center gap-2">
              Proceed to Checkout <ArrowRight className="w-4 h-4" />
            </Link>
            <Link to="/products" className="block text-center text-sm text-primary-600 font-medium mt-3 hover:text-primary-700">
              Continue Shopping
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
