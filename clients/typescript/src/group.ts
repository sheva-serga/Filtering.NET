import type { FilterChild, FilterGroup, FilterNode } from './types.js';

function isPresent(child: FilterChild): child is FilterNode {
  return child !== false && child !== null && child !== undefined && child !== '';
}

// An empty group is a validation error on the server (GroupEmpty), so it collapses to undefined and vanishes from its parent.
export function and(...children: FilterChild[]): FilterGroup | undefined {
  const presentChildren = children.filter(isPresent);
  return presentChildren.length === 0 ? undefined : { and: presentChildren };
}

export function or(...children: FilterChild[]): FilterGroup | undefined {
  const presentChildren = children.filter(isPresent);
  return presentChildren.length === 0 ? undefined : { or: presentChildren };
}

export function not(child: FilterChild): FilterGroup | undefined {
  return isPresent(child) ? { not: [child] } : undefined;
}
