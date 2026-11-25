#include "pch.h"
#include "ErrorLayer.hpp"

ErrorLayer::ErrorLayer(UserReportErrorPFN reportErrorFn)
{
	m_reportErrorFn = reportErrorFn;
}

ErrorLayer::~ErrorLayer()
{
	m_reportErrorFn = nullptr;
}

void ErrorLayer::reportError(physx::PxErrorCode::Enum code, const char* message, const char* file, int line)
{
	m_reportErrorFn(code, message, file, line);
}

extern "C"
{
	__declspec(dllexport) ErrorLayer* core_CreateError(UserReportErrorPFN* reportError)
	{
		return new ErrorLayer(reportError);
	}

	__declspec(dllexport) void core_DestroyError(ErrorLayer* layer)
	{
		delete layer;
	}
}