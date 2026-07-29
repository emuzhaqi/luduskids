import { useEffect, useState } from 'react';
import ProductCard from '../components/ProductCard';
import { getCategories, getProducts } from '../api/products';

export default function ProductListPage() {
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [categoryId, setCategoryId] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([getProducts(), getCategories()])
      .then(([productsData, categoriesData]) => {
        setProducts(productsData);
        setCategories(categoriesData);
      })
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <p>Loading toys...</p>;

  const filtered = categoryId
    ? products.filter((p) => p.categoryId === Number(categoryId))
    : products;

  return (
    <div>
      <h1>Shop</h1>
      <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
        <option value="">All categories</option>
        {categories.map((c) => (
          <option key={c.id} value={c.id}>{c.name}</option>
        ))}
      </select>

      {filtered.length === 0 ? (
        <p>No toys found.</p>
      ) : (
        <div className="product-grid">
          {filtered.map((product) => (
            <ProductCard key={product.id} product={product} />
          ))}
        </div>
      )}
    </div>
  );
}
