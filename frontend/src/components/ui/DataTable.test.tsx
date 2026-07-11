import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { DataTable, type Column } from '@/components/ui/DataTable';

type Row = { id: string; name: string; price: number };

const columns: Column<Row>[] = [
  { key: 'name', header: 'Name' },
  { key: 'price', header: 'Price', align: 'right' },
];

const data: Row[] = [
  { id: '1', name: 'Widget A', price: 19.99 },
  { id: '2', name: 'Widget B', price: 39.99 },
];

describe('DataTable', () => {
  it('renders column headers', () => {
    render(<DataTable columns={columns} data={data} keyExtractor={(r) => r.id} />);
    expect(screen.getByText('Name')).toBeInTheDocument();
    expect(screen.getByText('Price')).toBeInTheDocument();
  });

  it('renders data rows', () => {
    render(<DataTable columns={columns} data={data} keyExtractor={(r) => r.id} />);
    expect(screen.getByText('Widget A')).toBeInTheDocument();
    expect(screen.getByText('Widget B')).toBeInTheDocument();
  });

  it('shows empty message when data is empty', () => {
    render(
      <DataTable
        columns={columns}
        data={[]}
        keyExtractor={(r) => r.id}
        emptyMessage="No widgets found"
      />,
    );
    expect(screen.getByText('No widgets found')).toBeInTheDocument();
  });

  it('shows loading skeletons when loading=true', () => {
    const { container } = render(
      <DataTable columns={columns} data={[]} keyExtractor={(r) => r.id} loading />,
    );
    // Loading skeleton rows have animate-pulse class
    const pulsingEl = container.querySelector('.animate-pulse');
    expect(pulsingEl).toBeInTheDocument();
  });

  it('uses custom render for cells', () => {
    const cols: Column<Row>[] = [
      { key: 'name', header: 'Name', render: (row) => <strong>{row.name}</strong> },
    ];
    render(<DataTable columns={cols} data={data} keyExtractor={(r) => r.id} />);
    // custom render creates <strong> tags
    expect(screen.getByText('Widget A').tagName).toBe('STRONG');
  });
});
