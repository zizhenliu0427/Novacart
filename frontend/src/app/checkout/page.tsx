'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useCart } from '@/contexts/CartContext';
import { useAuth } from '@/contexts/AuthContext';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { EmptyState } from '@/components/ui/EmptyState';
import { CartIcon } from '@/components/icons';
import { formatPrice } from '@/types/product';
import { apiCall } from '@/lib/api';

type Address = {
  id: string;
  label: string;
  line1: string;
  line2?: string;
  city: string;
  state: string;
  postcode: string;
  country: string;
  isDefaultShipping: boolean;
};

export default function CheckoutPage() {
  const { user } = useAuth();
  const { cart, isLoading: isCartLoading, totalItems } = useCart();
  const router = useRouter();

  const [addresses, setAddresses] = useState<Address[]>([]);
  const [selectedAddressId, setSelectedAddressId] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Address creation form state
  const [showNewForm, setShowNewForm] = useState(false);
  const [label, setLabel] = useState('Home');
  const [line1, setLine1] = useState('');
  const [line2, setLine2] = useState('');
  const [city, setCity] = useState('');
  const [state, setState] = useState('');
  const [postcode, setPostcode] = useState('');
  const [country, setCountry] = useState('Australia');
  const [isDefaultShipping, setIsDefaultShipping] = useState(true);
  const [submittingAddress, setSubmittingAddress] = useState(false);
  const [addressError, setAddressError] = useState<string | null>(null);

  const [isPlacingOrder, setIsPlacingOrder] = useState(false);
  const [checkoutError, setCheckoutError] = useState<string | null>(null);

  useEffect(() => {
    if (!user) {
      router.push('/login?redirect=/checkout');
      return;
    }

    async function loadAddresses() {
      setLoading(true);
      try {
        const list = await apiCall<Address[]>('/address');
        setAddresses(list);
        
        // Default select default address
        const def = list.find((a) => a.isDefaultShipping) ?? list[0];
        if (def) {
          setSelectedAddressId(def.id);
        } else {
          setShowNewForm(true);
        }
      } catch (err) {
        setError('Failed to load addresses.');
      } finally {
        setLoading(false);
      }
    }
    loadAddresses();
  }, [user, router]);

  async function handleAddAddress(e: React.FormEvent) {
    e.preventDefault();
    setSubmittingAddress(true);
    setAddressError(null);
    try {
      const newAddr = await apiCall<Address>('/address', {
        method: 'POST',
        body: { label, line1, line2, city, state, postcode, country, isDefaultShipping, isDefaultBilling: isDefaultShipping },
      });
      setAddresses((prev) => [...prev, newAddr]);
      setSelectedAddressId(newAddr.id);
      setShowNewForm(false);
      
      // Reset form
      setLine1('');
      setLine2('');
      setCity('');
      setState('');
      setPostcode('');
    } catch (err: any) {
      setAddressError(err.message || 'Failed to add address.');
    } finally {
      setSubmittingAddress(false);
    }
  }

  async function handlePlaceOrder() {
    if (!selectedAddressId) {
      setCheckoutError('Please select a shipping address.');
      return;
    }
    setIsPlacingOrder(true);
    setCheckoutError(null);
    try {
      const res = await apiCall<{ redirectUrl: string }>('/checkout', {
        method: 'POST',
        body: {
          successUrl: window.location.origin + '/checkout/success',
          cancelUrl: window.location.origin + '/checkout/cancel',
          addressId: selectedAddressId,
        },
      });
      window.location.href = res.redirectUrl;
    } catch (err: any) {
      setCheckoutError(err.message || 'Failed to initiate checkout. Please try again.');
      setIsPlacingOrder(false);
    }
  }

  if (loading || isCartLoading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Checkout</h1>
        <div className="h-44 animate-pulse rounded-xl bg-bg-subtle" />
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Checkout</h1>
        <EmptyState
          icon={<CartIcon />}
          title="Your cart is empty"
          description="Browse products to add them to your cart before checking out."
          action={
            <Button onClick={() => router.push('/products')}>Browse Products</Button>
          }
        />
      </div>
    );
  }

  const subtotal = cart.subtotal;
  const shipping = subtotal >= 100 ? 0 : 10;
  const tax = Math.round((subtotal + shipping) * 0.1 * 100) / 100;
  const total = subtotal + shipping + tax;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Checkout</h1>
        <p className="text-sm text-ink-muted">Please complete your shipping and payment details.</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Left column: Address details */}
        <div className="space-y-6 lg:col-span-2">
          {/* Address Selector */}
          <Card className="p-5 space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-base font-semibold text-ink font-sans">Shipping Address</h2>
              {addresses.length > 0 && (
                <button
                  onClick={() => setShowNewForm(!showNewForm)}
                  className="text-xs text-accent hover:underline"
                >
                  {showNewForm ? 'Select address' : '+ Add new address'}
                </button>
              )}
            </div>

            {error && <p className="text-sm text-danger">{error}</p>}

            {!showNewForm && addresses.length > 0 ? (
              <div className="grid gap-3 sm:grid-cols-2">
                {addresses.map((addr) => (
                  <label
                    key={addr.id}
                    className={`relative flex flex-col gap-1 rounded-xl border p-4 cursor-pointer transition ${
                      selectedAddressId === addr.id
                        ? 'border-accent bg-accent-weak/20 ring-1 ring-accent'
                        : 'border-border bg-surface hover:bg-bg-subtle/50'
                    }`}
                  >
                    <input
                      type="radio"
                      name="shippingAddress"
                      checked={selectedAddressId === addr.id}
                      onChange={() => setSelectedAddressId(addr.id)}
                      className="sr-only"
                    />
                    <div className="flex items-center justify-between font-medium">
                      <span className="text-ink">{addr.label}</span>
                      {addr.isDefaultShipping && (
                        <span className="text-[10px] bg-bg-subtle text-ink-muted px-1.5 py-0.5 rounded font-medium">
                          Default
                        </span>
                      )}
                    </div>
                    <span className="text-sm text-ink-muted leading-relaxed">
                      {addr.line1}
                      {addr.line2 && `, ${addr.line2}`}
                      <br />
                      {addr.city}, {addr.state} {addr.postcode}
                      <br />
                      {addr.country}
                    </span>
                  </label>
                ))}
              </div>
            ) : (
              /* Add address form */
              <form onSubmit={handleAddAddress} className="space-y-3">
                {addressError && <p className="text-xs text-danger">{addressError}</p>}
                
                <div className="grid gap-3 sm:grid-cols-2">
                  <div>
                    <label className="block text-xs font-semibold text-ink-muted mb-1">Address Label</label>
                    <Input value={label} onChange={(e) => setLabel(e.target.value)} required placeholder="e.g. Home, Office" />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-ink-muted mb-1">Country</label>
                    <Input value={country} onChange={(e) => setCountry(e.target.value)} required />
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-ink-muted mb-1">Street Address</label>
                  <Input value={line1} onChange={(e) => setLine1(e.target.value)} required placeholder="123 Main St" />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-ink-muted mb-1">Apartment, suite, unit (optional)</label>
                  <Input value={line2} onChange={(e) => setLine2(e.target.value)} placeholder="Apt 4B" />
                </div>

                <div className="grid gap-3 sm:grid-cols-3">
                  <div>
                    <label className="block text-xs font-semibold text-ink-muted mb-1">City</label>
                    <Input value={city} onChange={(e) => setCity(e.target.value)} required />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-ink-muted mb-1">State / Territory</label>
                    <Input value={state} onChange={(e) => setState(e.target.value)} required />
                  </div>
                  <div>
                    <label className="block text-xs font-semibold text-ink-muted mb-1">Postcode</label>
                    <Input value={postcode} onChange={(e) => setPostcode(e.target.value)} required />
                  </div>
                </div>

                <label className="flex items-center gap-2 text-sm text-ink-muted cursor-pointer py-1">
                  <input
                    type="checkbox"
                    checked={isDefaultShipping}
                    onChange={(e) => setIsDefaultShipping(e.target.checked)}
                    className="rounded border-border text-accent focus:ring-accent"
                  />
                  Set as default shipping address
                </label>

                <div className="flex gap-2 justify-end pt-2">
                  {addresses.length > 0 && (
                    <Button variant="secondary" size="sm" type="button" onClick={() => setShowNewForm(false)}>
                      Cancel
                    </Button>
                  )}
                  <Button size="sm" disabled={submittingAddress} type="submit">
                    {submittingAddress ? 'Saving…' : 'Save Address'}
                  </Button>
                </div>
              </form>
            )}
          </Card>

          {/* Payment Method placeholder */}
          <Card className="p-5 space-y-3">
            <h2 className="text-base font-semibold text-ink">Payment Method</h2>
            <div className="rounded-xl border border-accent bg-accent-weak/20 p-4 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <span className="font-semibold text-accent text-sm">Stripe</span>
                <span className="text-xs text-ink-muted">Secure credit card checkout</span>
              </div>
              <span className="h-2 w-2 rounded-full bg-accent" />
            </div>
          </Card>
        </div>

        {/* Right column: Cart Summary & Place Order */}
        <div className="lg:col-span-1">
          <Card className="p-5 space-y-4">
            <h2 className="font-semibold text-ink">Order Summary</h2>

            {/* Order Items */}
            <div className="divide-y divide-border max-h-48 overflow-y-auto">
              {cart.items.map((item) => (
                <div key={item.id} className="py-2.5 flex justify-between gap-3 text-sm">
                  <div>
                    <p className="font-medium text-ink line-clamp-1">{item.productName}</p>
                    <p className="text-xs text-ink-muted">Qty: {item.quantity}</p>
                  </div>
                  <span className="tnum font-semibold text-ink shrink-0">{formatPrice(item.lineTotal)}</span>
                </div>
              ))}
            </div>

            <div className="border-t border-border pt-4 space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-ink-muted">Subtotal</span>
                <span className="tnum font-medium text-ink">{formatPrice(subtotal)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-ink-muted">Shipping</span>
                <span className="tnum font-medium text-ink">{shipping === 0 ? 'Free' : formatPrice(shipping)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-ink-muted">Estimated Tax (10% GST)</span>
                <span className="tnum font-medium text-ink">{formatPrice(tax)}</span>
              </div>
            </div>

            <div className="border-t border-border pt-4 flex justify-between text-base font-bold text-ink">
              <span>Total</span>
              <span className="tnum">{formatPrice(total)}</span>
            </div>

            {checkoutError && (
              <p className="text-xs text-danger text-center bg-red-50 dark:bg-red-950/20 p-2.5 rounded border border-red-100 dark:border-red-900/30">
                {checkoutError}
              </p>
            )}

            <Button
              className="w-full"
              disabled={isPlacingOrder || !selectedAddressId}
              onClick={handlePlaceOrder}
            >
              {isPlacingOrder ? 'Redirecting to payment...' : 'Place Order'}
            </Button>
          </Card>
        </div>
      </div>
    </div>
  );
}
