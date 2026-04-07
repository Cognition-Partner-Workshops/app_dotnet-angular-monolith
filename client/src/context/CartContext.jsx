import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import api from '../api';
import { useAuth } from './AuthContext';
import toast from 'react-hot-toast';

const CartContext = createContext(null);

export function CartProvider({ children }) {
  const { token } = useAuth();
  const [cart, setCart] = useState({ items: [], total: 0, count: 0 });
  const [loading, setLoading] = useState(false);

  const fetchCart = useCallback(async () => {
    if (!token) {
      setCart({ items: [], total: 0, count: 0 });
      return;
    }
    try {
      setLoading(true);
      const res = await api.get('/cart');
      setCart(res.data);
    } catch (err) {
      console.error('Failed to fetch cart:', err);
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    fetchCart();
  }, [fetchCart]);

  const addToCart = async (productId, quantity = 1) => {
    try {
      await api.post('/cart', { product_id: productId, quantity });
      await fetchCart();
      toast.success('Added to cart!');
    } catch (err) {
      toast.error(err.response?.data?.error || 'Failed to add to cart');
    }
  };

  const updateQuantity = async (cartItemId, quantity) => {
    try {
      await api.put(`/cart/${cartItemId}`, { quantity });
      await fetchCart();
    } catch (err) {
      toast.error(err.response?.data?.error || 'Failed to update cart');
    }
  };

  const removeFromCart = async (cartItemId) => {
    try {
      await api.delete(`/cart/${cartItemId}`);
      await fetchCart();
      toast.success('Item removed');
    } catch (err) {
      toast.error('Failed to remove item');
    }
  };

  const clearCart = async () => {
    try {
      await api.delete('/cart');
      setCart({ items: [], total: 0, count: 0 });
    } catch (err) {
      toast.error('Failed to clear cart');
    }
  };

  const value = { cart, loading, addToCart, updateQuantity, removeFromCart, clearCart, fetchCart };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export const useCart = () => {
  const context = useContext(CartContext);
  if (!context) throw new Error('useCart must be used within CartProvider');
  return context;
};
