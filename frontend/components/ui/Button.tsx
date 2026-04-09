import * as React from "react";
import { Slot } from "@radix-ui/react-slot";
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-lg text-sm font-semibold transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/40 disabled:pointer-events-none disabled:opacity-60",
  {
    variants: {
      variant: {
        default: "border border-transparent bg-primary text-primary-foreground hover:bg-primary/90",
        solid:
          "border border-transparent bg-primary text-primary-foreground hover:bg-primary/90",
        secondary: "border border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/85",
        outline:
          "border border-border bg-transparent text-foreground hover:bg-accent hover:text-accent-foreground",
        ghost: "border border-transparent bg-transparent text-foreground hover:bg-accent hover:text-accent-foreground",
        link: "border border-transparent text-primary underline-offset-4 hover:underline",
        destructive: "border border-transparent bg-destructive text-destructive-foreground hover:bg-destructive/90",
        danger: "border border-transparent bg-destructive text-destructive-foreground hover:bg-destructive/90",
      },
      size: {
        sm: "h-9 px-3 text-xs",
        md: "h-10 px-4",
        lg: "h-11 px-6 text-base",
        icon: "h-10 w-10",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "md",
    },
  },
);

type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean;
    isLoading?: boolean;
  };

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { className, variant, size, asChild = false, isLoading = false, disabled, children, ...props },
  ref,
) {
  const Comp = asChild ? Slot : "button";
  const isDisabled = disabled || isLoading;

  if (asChild) {
    return (
      <Comp
        ref={ref}
        className={cn(buttonVariants({ variant, size }), className)}
        disabled={isDisabled}
        {...props}
      >
        {children}
      </Comp>
    );
  }

  return (
    <Comp
      ref={ref}
      className={cn(buttonVariants({ variant, size }), className)}
      disabled={isDisabled}
      {...props}
    >
      {isLoading ? (
        <span
          className="h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent"
          aria-hidden="true"
        />
      ) : null}
      {children}
    </Comp>
  );
});

export { buttonVariants };