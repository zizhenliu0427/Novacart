import { ComingSoon } from '@/components/ui/ComingSoon';

export default function AdminProductsPage() {
  return (
    <ComingSoon title="Products" item="P2-8">
      Product CRUD + inventory: create/edit/deactivate products, adjust stock, see low-stock items.
      Calls <code>/api/admin/products</code> (RBAC-guarded).
    </ComingSoon>
  );
}
