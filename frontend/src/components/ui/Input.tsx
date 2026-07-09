import React, { useId } from 'react';

interface InputProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'size'> {
  label?: string;
  error?: string;
  helperText?: string;
  size?: 'sm' | 'md' | 'lg';
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, helperText, size = 'md', className = '', type = 'text', id, ...props }, ref) => {
    const fallbackId = useId();
    const inputId = id || fallbackId;

    const sizeClasses = {
      sm: 'h-8 px-2 text-xs rounded-md',
      md: 'h-10 px-3 text-sm rounded-lg',
      lg: 'h-12 px-4 text-base rounded-xl',
    };

    const stateClasses = error
      ? 'border-danger focus:ring-danger/20 focus:border-danger'
      : 'border-border focus:ring-accent/20 focus:border-accent';

    return (
      <div className="w-full space-y-1.5 text-left">
        {label && (
          <label htmlFor={inputId} className="block text-xs font-semibold text-ink-muted uppercase tracking-wider">
            {label}
          </label>
        )}
        <input
          ref={ref}
          id={inputId}
          type={type}
          className={`w-full border bg-surface text-ink placeholder:text-ink-muted focus:outline-none focus:ring-2 transition disabled:opacity-40 disabled:bg-bg-subtle/50 ${sizeClasses[size]} ${stateClasses} ${className}`}
          {...props}
        />
        {error && (
          <p className="text-xs text-danger font-medium">
            {error}
          </p>
        )}
        {!error && helperText && (
          <p className="text-xs text-ink-muted">
            {helperText}
          </p>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';
export default Input;
