import { HttpError, ValidationError, type ValidationErrorModel } from "./customErrors";

function isValidationError(data: any): data is ValidationErrorModel {
  return data && typeof data.errorStates === "object" && !Array.isArray(data.errors);
}

function throwHttpError(response: Response, json?: any): never {
  if (json && isValidationError(json)) {
    throw new ValidationError(json);
  }

  if (json) {
    throw new HttpError(json);
  }

  throw new HttpError({
    message: `HTTP ${response.status} ${response.statusText}`,
    statusCode: response.status
  });
}

async function handleResponse<T>(response: Response): Promise<T> {
  const text = await response.text();

  if (!text || text.trim().length === 0) {
    if (!response.ok) {
      throwHttpError(response);
    }

    return undefined as T;
  }

  let json: any;

  try {
    json = JSON.parse(text);
  } catch {
    if (!response.ok) {
      throw new HttpError({
        message: text || `HTTP ${response.status} ${response.statusText}`,
        statusCode: response.status
      });
    }

    return text as T;
  }

  if (!response.ok) {
    throwHttpError(response, json);
  }

  return json as T;
}

export async function get<T>(url: string): Promise<T> {
  const response = await fetch(url, {
    credentials: "include",
    method: "GET"
  });

  return handleResponse<T>(response);
}

export async function del<T>(url: string, bodyContent: any | undefined): Promise<T> {
  const response = await fetch(url, {
    credentials: "include",
    method: "DELETE",
    body: bodyContent ? JSON.stringify(bodyContent) : undefined,
    headers: {
      "Content-Type": "application/json"
    }
  });

  return handleResponse<T>(response);
}

export async function post<T>(url: string, bodyContent: any | undefined): Promise<T> {
  const response = await fetch(url, {
    credentials: "include",
    method: "POST",
    body: bodyContent ? JSON.stringify(bodyContent) : undefined,
    headers: {
      "Content-Type": "application/json"
    }
  });

  return handleResponse<T>(response);
}

export async function patch<T>(url: string, bodyContent: any | undefined): Promise<T> {
  const response = await fetch(url, {
    credentials: "include",
    method: "PATCH",
    body: bodyContent ? JSON.stringify(bodyContent) : undefined,
    headers: {
      "Content-Type": "application/json"
    }
  });

  return handleResponse<T>(response);
}

export async function put<T>(url: string, bodyContent: any | undefined): Promise<T> {
  const response = await fetch(url, {
    credentials: "include",
    method: "PUT",
    body: bodyContent ? JSON.stringify(bodyContent) : undefined,
    headers: {
      "Content-Type": "application/json"
    }
  });

  return handleResponse<T>(response);
}