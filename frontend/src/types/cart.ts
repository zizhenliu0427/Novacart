export interface CartItem {
  id: string;
  productId: string;
  productName: string;
  productSlug?: string;
  unitPrice: number;
  currency: string;
  quantity: number;
  lineTotal: number;
  stockQuantity: number;
}

export interface Cart {
  id: string;
  items: CartItem[];
  subtotal: number;
  totalItems: number;
}
