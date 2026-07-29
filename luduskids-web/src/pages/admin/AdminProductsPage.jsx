import { useEffect, useState } from 'react';
import {
  createCategory,
  createProduct,
  deleteProduct,
  getCategories,
  getProducts,
  updateProduct,
} from '../../api/products';

const emptyForm = { name: '', description: '', price: '', imageUrl: '', stockQuantity: '', categoryId: '' };

export default function AdminProductsPage() {
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);
  const [newCategoryName, setNewCategoryName] = useState('');

  const load = async () => {
    setProducts(await getProducts());
    setCategories(await getCategories());
  };

  useEffect(() => {
    load();
  }, []);

  const handleChange = (e) => setForm({ ...form, [e.target.name]: e.target.value });

  const handleSubmit = async (e) => {
    e.preventDefault();
    const payload = {
      name: form.name,
      description: form.description,
      price: Number(form.price),
      imageUrl: form.imageUrl,
      stockQuantity: Number(form.stockQuantity),
      categoryId: Number(form.categoryId),
    };

    if (editingId) {
      await updateProduct(editingId, payload);
    } else {
      await createProduct(payload);
    }

    setForm(emptyForm);
    setEditingId(null);
    await load();
  };

  const handleEdit = (product) => {
    setEditingId(product.id);
    setForm({
      name: product.name,
      description: product.description,
      price: product.price,
      imageUrl: product.imageUrl,
      stockQuantity: product.stockQuantity,
      categoryId: product.categoryId,
    });
  };

  const handleDelete = async (id) => {
    await deleteProduct(id);
    await load();
  };

  const handleAddCategory = async (e) => {
    e.preventDefault();
    if (!newCategoryName.trim()) return;
    await createCategory({ name: newCategoryName });
    setNewCategoryName('');
    await load();
  };

  return (
    <div className="admin-page">
      <h1>Manage products</h1>

      <section>
        <h2>Categories</h2>
        <ul>
          {categories.map((c) => <li key={c.id}>{c.name}</li>)}
        </ul>
        <form onSubmit={handleAddCategory} className="inline-form">
          <input
            placeholder="New category name"
            value={newCategoryName}
            onChange={(e) => setNewCategoryName(e.target.value)}
          />
          <button type="submit">Add category</button>
        </form>
      </section>

      <section>
        <h2>{editingId ? 'Edit product' : 'Add product'}</h2>
        <form onSubmit={handleSubmit} className="admin-form">
          <label>
            Name
            <input name="name" value={form.name} onChange={handleChange} required />
          </label>
          <label>
            Description
            <textarea name="description" value={form.description} onChange={handleChange} required />
          </label>
          <label>
            Price
            <input name="price" type="number" step="0.01" min="0" value={form.price} onChange={handleChange} required />
          </label>
          <label>
            Image URL
            <input name="imageUrl" value={form.imageUrl} onChange={handleChange} required />
          </label>
          <label>
            Stock quantity
            <input name="stockQuantity" type="number" min="0" value={form.stockQuantity} onChange={handleChange} required />
          </label>
          <label>
            Category
            <select name="categoryId" value={form.categoryId} onChange={handleChange} required>
              <option value="">Select a category</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>{c.name}</option>
              ))}
            </select>
          </label>
          <button type="submit">{editingId ? 'Save changes' : 'Add product'}</button>
          {editingId && (
            <button type="button" onClick={() => { setEditingId(null); setForm(emptyForm); }}>
              Cancel
            </button>
          )}
        </form>
      </section>

      <section>
        <h2>All products</h2>
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Category</th>
              <th>Price</th>
              <th>Stock</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {products.map((p) => (
              <tr key={p.id}>
                <td>{p.name}</td>
                <td>{p.categoryName}</td>
                <td>${p.price.toFixed(2)}</td>
                <td>{p.stockQuantity}</td>
                <td>
                  <button onClick={() => handleEdit(p)}>Edit</button>
                  <button onClick={() => handleDelete(p.id)}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}
