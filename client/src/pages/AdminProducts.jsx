import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Edit2, Trash2, ArrowLeft, X, Save } from 'lucide-react';
import api from '../api';
import toast from 'react-hot-toast';

export default function AdminProducts() {
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form, setForm] = useState({
    name: '', description: '', price: '', unit: 'piece', stock: '', category_id: '', image_url: '',
  });

  useEffect(() => {
    Promise.all([
      api.get('/admin/products'),
      api.get('/products/categories'),
    ])
      .then(([productsRes, categoriesRes]) => {
        setProducts(productsRes.data);
        setCategories(categoriesRes.data);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  const openCreate = () => {
    setEditing(null);
    setForm({ name: '', description: '', price: '', unit: 'piece', stock: '', category_id: '', image_url: '' });
    setShowModal(true);
  };

  const openEdit = (product) => {
    setEditing(product);
    setForm({
      name: product.name,
      description: product.description || '',
      price: String(product.price),
      unit: product.unit,
      stock: String(product.stock),
      category_id: String(product.category_id || ''),
      image_url: product.image_url || '',
    });
    setShowModal(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    const data = {
      ...form,
      price: parseFloat(form.price),
      stock: parseInt(form.stock) || 0,
      category_id: form.category_id ? parseInt(form.category_id) : null,
    };

    try {
      if (editing) {
        await api.put(`/admin/products/${editing.id}`, data);
        toast.success('Product updated');
      } else {
        await api.post('/admin/products', data);
        toast.success('Product created');
      }
      // Refresh
      const res = await api.get('/admin/products');
      setProducts(res.data);
      setShowModal(false);
    } catch (err) {
      toast.error(err.response?.data?.error || 'Failed to save product');
    }
  };

  const handleDelete = async (id) => {
    if (!confirm('Are you sure you want to deactivate this product?')) return;
    try {
      await api.delete(`/admin/products/${id}`);
      setProducts(products.map(p => p.id === id ? { ...p, is_active: 0 } : p));
      toast.success('Product deactivated');
    } catch (err) {
      toast.error('Failed to delete product');
    }
  };

  const update = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  if (loading) {
    return (
      <div className="min-h-[60vh] flex items-center justify-center">
        <div className="animate-spin rounded-full h-10 w-10 border-4 border-primary-600 border-t-transparent"></div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="flex items-center justify-between mb-8">
        <div className="flex items-center gap-4">
          <Link to="/admin" className="text-gray-400 hover:text-primary-600">
            <ArrowLeft className="w-5 h-5" />
          </Link>
          <div>
            <h1 className="text-3xl font-bold text-gray-800">Manage Products</h1>
            <p className="text-gray-500 mt-1">{products.length} products total</p>
          </div>
        </div>
        <button onClick={openCreate} className="btn-primary flex items-center gap-2">
          <Plus className="w-5 h-5" /> Add Product
        </button>
      </div>

      {/* Products Table */}
      <div className="card overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="text-left text-xs font-medium text-gray-500 uppercase tracking-wider bg-gray-50">
                <th className="px-6 py-3">Product</th>
                <th className="px-6 py-3">Category</th>
                <th className="px-6 py-3">Price</th>
                <th className="px-6 py-3">Stock</th>
                <th className="px-6 py-3">Status</th>
                <th className="px-6 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {products.map((product) => (
                <tr key={product.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-3">
                      <img
                        src={product.image_url}
                        alt={product.name}
                        className="w-10 h-10 rounded-lg object-cover bg-gray-100"
                        onError={(e) => { e.target.src = `https://ui-avatars.com/api/?name=${encodeURIComponent(product.name)}&background=dcfce7&color=166534&size=80`; }}
                      />
                      <div>
                        <p className="font-medium text-gray-800 text-sm">{product.name}</p>
                        <p className="text-xs text-gray-400">ID: {product.id}</p>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 text-sm text-gray-600">{product.category_name || '-'}</td>
                  <td className="px-6 py-4 text-sm font-medium text-gray-800">${product.price.toFixed(2)}</td>
                  <td className="px-6 py-4">
                    <span className={`text-sm font-medium ${product.stock < 10 ? 'text-red-600' : 'text-gray-800'}`}>
                      {product.stock}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <span className={`badge ${product.is_active ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                      {product.is_active ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-right">
                    <div className="flex items-center justify-end gap-2">
                      <button onClick={() => openEdit(product)} className="p-1.5 text-gray-400 hover:text-blue-600 transition-colors">
                        <Edit2 className="w-4 h-4" />
                      </button>
                      <button onClick={() => handleDelete(product.id)} className="p-1.5 text-gray-400 hover:text-red-600 transition-colors">
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="bg-white rounded-xl shadow-xl max-w-lg w-full max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between p-6 border-b border-gray-100">
              <h2 className="text-lg font-bold text-gray-800">{editing ? 'Edit Product' : 'Add New Product'}</h2>
              <button onClick={() => setShowModal(false)} className="text-gray-400 hover:text-gray-600">
                <X className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Product Name *</label>
                <input type="text" required value={form.name} onChange={update('name')} className="input-field" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                <textarea value={form.description} onChange={update('description')} className="input-field" rows="3" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Price ($) *</label>
                  <input type="number" step="0.01" required value={form.price} onChange={update('price')} className="input-field" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Stock *</label>
                  <input type="number" required value={form.stock} onChange={update('stock')} className="input-field" />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Category</label>
                  <select value={form.category_id} onChange={update('category_id')} className="input-field">
                    <option value="">Select category</option>
                    {categories.map(cat => (
                      <option key={cat.id} value={cat.id}>{cat.name}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Unit</label>
                  <select value={form.unit} onChange={update('unit')} className="input-field">
                    <option value="piece">Piece</option>
                    <option value="bottle">Bottle</option>
                    <option value="pack">Pack</option>
                    <option value="cup">Cup</option>
                    <option value="jar">Jar</option>
                    <option value="tub">Tub</option>
                    <option value="kg">Kg</option>
                    <option value="liter">Liter</option>
                  </select>
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Image URL</label>
                <input type="url" value={form.image_url} onChange={update('image_url')} className="input-field" placeholder="https://..." />
              </div>
              <div className="flex gap-3 pt-2">
                <button type="button" onClick={() => setShowModal(false)} className="btn-secondary flex-1">Cancel</button>
                <button type="submit" className="btn-primary flex-1 flex items-center justify-center gap-2">
                  <Save className="w-4 h-4" /> {editing ? 'Update' : 'Create'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
