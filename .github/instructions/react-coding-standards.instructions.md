---
description: 'Comprehensive React and TypeScript coding standards for building robust, maintainable, and AI-ready applications. This guide enforces automated code quality, type safety, and best practices to support modern development workflows.'
applyTo: '**/*.tsx, **/*.ts'
---

# React Development Instructions (TypeScript)

Instructions for building high-quality React applications with TypeScript, modern patterns, hooks, and best practices following the official React documentation at https://react.dev.

## Project Context

**This document assumes TypeScript is used throughout the entire project.**

- Latest stable React version
- TypeScript for type safety and enhanced developer experience
- Functional components with hooks as default
- Follow React's official style guide and TypeScript best practices
- Use modern build tools (Vite, Create React App)
- Implement proper component composition and reusability patterns
- Run npm audit fix before every check in

## Development Standards

### Architecture

- Use functional components with hooks as the primary pattern
- Implement component composition over inheritance
- Organize components by feature or domain for scalability
- Separate presentational and container components clearly
- Use custom hooks for reusable stateful logic
- Implement proper component hierarchies with clear data flow
- Each component should follow the single responsibility principle (SRP); move complex logic to custom hooks or utilities.
- Limit depth of component nesting and prop drilling; if props are passed through many layers, consider using context, composition, or more localized state.

### TypeScript Best Practices

- Use interfaces and types for all props, state, and component definitions
- Define proper types for event handlers, refs, and custom hooks
- Implement generic components where appropriate for reusability
- Use strict mode in `tsconfig.json` for maximum type safety
- Leverage React's built-in types (`React.FC`, `React.ComponentProps`, etc.)
- Create union types for component variants and states, and enforce discriminant properties (e.g., `type: 'foo' | 'bar'`) for safe narrowing.
- Use `as const` assertions for literal types and readonly arrays
- Implement proper type guards and discriminated unions
- Export types and interfaces for reuse across components
- Ban usage of `any`; require `unknown` instead, with type guards to narrow types. Enforce via ESLint rule.
- Use TypeScript utility types (`Partial<T>`, `Required<T>`, `Pick<T, ...>`, `Omit<T, ...>`) when modifying or extending interfaces. Avoid reinventing patterns.
- Explicitly handle possible `null`/`undefined` values using optional chaining, defensive checks, or fallback defaults when types allow them.

### Component Design

- Use descriptive and consistent naming conventions (PascalCase for components, camelCase for functions and variables)
- Implement proper prop validation with interfaces
- Design components to be testable and reusable
- Keep components small and follow the single responsibility principle; perform self-review and remove leftover console.logs, dead code, and obvious formatting issues before submitting.
- Use composition patterns (render props, children as functions)
- Export component types for external consumption
- Use proper naming conventions for files and exports

### State Management

- Use `useState` with proper typing for local component state
- Implement `useReducer` with typed actions and state for complex state logic
- Leverage `useContext` with properly typed context for sharing state across component trees
- Consider external state management (Redux Toolkit) for complex applications
- Implement proper state normalization and typed data structures
- Define clear interfaces for all state shapes and actions

### Hooks and Effects

- Use `useEffect` with proper dependency arrays and typing to avoid infinite loops
- Implement cleanup functions in effects to prevent memory leaks
- Use `useMemo` and `useCallback` with proper generics for performance optimization when needed
- Create custom hooks with proper return types for reusable stateful logic
- Follow the rules of hooks (only call at the top level)
- Use `useRef` with proper typing for accessing DOM elements and storing mutable values
- Type all custom hooks with proper input and return type definitions

### Styling

- Prefer modern CSS-in-JSS solution instead of separate CSS Modules
- Implement responsive design that works up to 370px screen widths
- Follow BEM methodology or similar naming conventions for CSS classes
- Use CSS custom properties (variables) for theming with type definitions
- Implement consistent spacing, typography, and color systems
- Ensure accessibility with proper ARIA attributes and semantic HTML

### Performance Optimization

- Use `React.memo` for component memoization when appropriate
- Implement code splitting with `React.lazy` and `Suspense`; identify logical boundaries for splitting (route-based, or component-heavy/rarely used components) and ensure fallback UIs are acceptable and tested.
- Optimize bundle size with tree shaking and dynamic imports
- Use `useMemo` and `useCallback` judiciously to prevent unnecessary re-renders
- Ensure `useEffect`, `useCallback`, and `useMemo` dependencies are complete and correct; use lint rules (eslint-plugin-react-hooks) to enforce.
- Implement virtual scrolling for large lists
- Profile components with React DevTools to identify performance bottlenecks

### Data Fetching

- Use modern data fetching libraries (React Query) with generics
- Implement proper loading, error, and success states with typed interfaces; always handle loading, error, and empty states for all data fetching, and define well what happens (loading indicator, error message/retry, fallback for empty or null/undefined data).
- Handle race conditions and request cancellation with proper typing
- Use optimistic updates for better user experience with type-safe implementations
- Implement proper caching strategies with typed cache keys
- Handle offline scenarios and network errors gracefully with typed error handling
- Define clear API response types and request payload interfaces

### Error Handling

- Implement Error Boundaries for component-level error handling; place error boundaries for parts of the component tree, especially when integrating third-party or asynchronous operations. Ensure fallback UI is meaningful.
- Use proper error states in data fetching
- Implement fallback UI for error scenarios
- Log errors appropriately for debugging
- Handle async errors in effects and event handlers
- Provide meaningful error messages to users; avoid leaking internal error structures such as stack traces or backend internals in user-facing messages.
- Stringify all error objects before logging
- Wherever there is a batch call, iterate over the responses and log each error separately.

### Logging

- Provide a single logging abstraction wrapping Application Insights; disallow direct usage of raw logging APIs in application code.
- Emit structured events with stable names and small, sanitized property sets (omit or hash PII, avoid high-cardinality dynamic values).
- Standardize levels (trace, debug, info, warn, error) and strip trace/debug from production bundles; enforce via lint rules.
- Propagate a correlation ID per user interaction/request and include it on every event, metric, and exception for end-to-end traceability.
- Capture performance timings (API calls, critical UI flows) as metrics and log failures with duration, sanitized error metadata, and correlation ID.
- Apply rate limiting / deduplication for repetitive warnings or errors to protect telemetry quotas and reduce noise.

### Forms and Validation

- Use controlled components for form inputs with proper typing
- Handle form submission and error states appropriately with typed form data
- Implement accessibility features for forms (labels, ARIA attributes)
- Use debounced validation for better user experience with typed validation functions

### Routing

- Use React Router for client-side routing
- Implement nested routes and route protection
- Handle route parameters and query strings properly
- Implement lazy loading for route-based code splitting
- Use proper navigation patterns and back button handling
- Implement breadcrumbs and navigation state management

### Testing

- Write unit tests for components using React Testing Library
- Test component behavior, not implementation details, with proper type assertions
- Use Jest for test runner and assertion library with full configuration
- Implement integration tests for complex component interactions with typed test utilities
- Mock external dependencies and API calls appropriately with typed mocks
- Test accessibility features and keyboard navigation
- Use type safety in test files to maintain consistency

### Security

- Sanitize user inputs and external data before rendering, especially when using `dangerouslySetInnerHTML` or injecting HTML.
- Validate and escape data before rendering
- Use HTTPS for all external API calls
- Implement proper authentication and authorization patterns
- Avoid storing sensitive data in localStorage or sessionStorage
- Use Content Security Policy (CSP) headers
- Provide data-id attribute to all controls.
- Avoid leaking internal error structures; error messages shown to users should not expose stack traces or backend internals.

### Accessibility

- Use semantic HTML elements appropriately
- Implement proper ARIA attributes and roles
- Ensure keyboard navigation works for all interactive elements
- Provide alt text for images and descriptive text for icons
- Implement proper color contrast ratios
- Test with screen readers and accessibility tools

## Implementation Process

1. Plan component architecture and data flow with interfaces
2. Set up project structure with proper folder organization and configuration
3. Define interfaces, types, and enums for all data structures
4. Implement core components with proper typing and styling
5. Add state management and data fetching logic with type safety
6. Implement routing and navigation with typed route parameters
7. Add form handling and validation with typed schemas
8. Implement error handling and loading states with typed error boundaries and meaningful fallback UIs
9. Add testing coverage for components and functionality
10. Optimize performance and bundle size while maintaining type safety
11. Ensure accessibility compliance with typed ARIA attributes

## Additional Guidelines

- Follow React's naming conventions (PascalCase for components, camelCase for functions)
- Implement proper code splitting and lazy loading strategies
- Document complex components and custom hooks with JSDoc and type annotations
- Use ESLint and Prettier with appropriate rules for consistent code formatting; enforce linting and formatting in CI, and fail builds on violations. Ensure consistent configuration and auto-fix on commit where possible.
- Limit PR sizes; encourage small, self-contained changes for easier review.
- Keep dependencies up to date and audit for security vulnerabilities
- Implement proper environment configuration for different deployment stages with typed env variables
- Use React Developer Tools for debugging and performance analysis
- Maintain strict configuration for maximum type safety
- Export all necessary types and interfaces for component library usage

## File Organization

- Use `.tsx` extension for files containing JSX
- Use `.ts` extension for utility functions, hooks, and type definitions
- Create separate `types.ts` or `interfaces.ts` files for shared type definitions
- Organize types alongside their related components

## Common Patterns

- Higher-Order Components (HOCs) for cross-cutting concerns
- Render props pattern for component composition
- Compound components for related functionality
- Provider pattern for context-based state sharing
- Container/Presentational component separation for complex UIs
- Custom hooks for reusable logic extraction
