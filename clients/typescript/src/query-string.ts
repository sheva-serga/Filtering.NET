import { request, type RequestParts } from './request.js';
import type { FilterNode, FilterRequest, SortItem } from './types.js';

type QueryParameter = 'where' | 'sort' | 'page' | 'pageSize';

export class FilterQueryStringError extends Error {
  readonly parameter: QueryParameter;

  constructor(parameter: QueryParameter, message: string) {
    super(message);
    this.name = 'FilterQueryStringError';
    this.parameter = parameter;
  }
}

export function toQueryString(filterRequest: FilterRequest): string {
  const parameters = new URLSearchParams();
  if (filterRequest.where !== undefined) {
    parameters.append('where', JSON.stringify(filterRequest.where));
  }
  for (const item of filterRequest.sort ?? []) {
    parameters.append('sort', item.dir === undefined ? item.field : `${item.field}:${item.dir}`);
  }
  if (filterRequest.page !== undefined) {
    parameters.append('page', String(filterRequest.page));
  }
  if (filterRequest.pageSize !== undefined) {
    parameters.append('pageSize', String(filterRequest.pageSize));
  }
  return parameters.toString();
}

// URLSearchParams already drops one leading "?" from a string.
export function fromQueryString(query: string | URLSearchParams): FilterRequest {
  const parameters = typeof query === 'string' ? new URLSearchParams(query) : query;
  const parts: RequestParts = {};

  const whereText = readSingle(parameters, 'where');
  if (whereText !== undefined) {
    parts.where = parseWhere(whereText);
  }
  const sortTexts = parameters.getAll('sort');
  if (sortTexts.length > 0) {
    parts.sort = sortTexts.map(parseSortItem);
  }
  const pageText = readSingle(parameters, 'page');
  if (pageText !== undefined) {
    parts.page = parseInteger('page', pageText);
  }
  const pageSizeText = readSingle(parameters, 'pageSize');
  if (pageSizeText !== undefined) {
    parts.pageSize = parseInteger('pageSize', pageSizeText);
  }

  return request(parts);
}

// The server cannot bind two values into one scalar either, so taking the first would hide a real mismatch.
function readSingle(parameters: URLSearchParams, parameter: Exclude<QueryParameter, 'sort'>): string | undefined {
  const values = parameters.getAll(parameter);
  if (values.length > 1) {
    throw new FilterQueryStringError(parameter, `'${parameter}' must appear at most once, got ${values.length} values.`);
  }
  return values[0];
}

function parseWhere(text: string): FilterNode {
  let parsed: unknown;
  try {
    parsed = JSON.parse(text);
  } catch {
    throw new FilterQueryStringError('where', "'where' is not valid JSON.");
  }
  if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
    throw new FilterQueryStringError('where', "'where' must be a JSON object.");
  }
  return parsed as FilterNode;
}

function parseSortItem(text: string): SortItem {
  const separatorIndex = text.lastIndexOf(':');
  const fieldName = separatorIndex < 0 ? text : text.slice(0, separatorIndex);
  if (fieldName.trim() === '') {
    throw new FilterQueryStringError('sort', `Sort item '${text}' has no field name.`);
  }
  if (separatorIndex < 0) {
    return { field: fieldName };
  }
  const direction = text.slice(separatorIndex + 1).toLowerCase();
  if (direction !== 'asc' && direction !== 'desc') {
    throw new FilterQueryStringError('sort', `Sort item '${text}' must end with ':asc' or ':desc'.`);
  }
  return { field: fieldName, dir: direction };
}

function parseInteger(parameter: 'page' | 'pageSize', text: string): number {
  if (!/^-?\d+$/.test(text)) {
    throw new FilterQueryStringError(parameter, `'${parameter}' must be an integer, got '${text}'.`);
  }
  return Number(text);
}
