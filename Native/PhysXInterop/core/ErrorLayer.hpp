#pragma once

#include <foundation/PxErrorCallback.h>

typedef void* UserReportErrorPFN(physx::PxErrorCode::Enum code, const char* message, const char* filename, int line);

class ErrorLayer : public physx::PxErrorCallback
{
public:
	ErrorLayer(UserReportErrorPFN reportErrorFn);
	~ErrorLayer();

	void reportError(physx::PxErrorCode::Enum code, const char* message, const char* file, int line) override;
private:
	UserReportErrorPFN* m_reportErrorFn;
};

extern "C"
{
	__declspec(dllexport) ErrorLayer* core_CreateError(UserReportErrorPFN* reportError);
	__declspec(dllexport) void core_DestroyError(ErrorLayer* layer);
}