#include <spirv-tools/optimizer.hpp>

typedef void* (*SPIRV_OptimizeAlloc)(size_t size);

#pragma pack(1)
struct SPIRV_OptimizeOut
{
	SPIRV_OptimizeAlloc Alloc;

	uint32_t* InBinary;
	size_t InSize;

	uint32_t* OutBinary;
	size_t OutSize;
};
#pragma pack(pop)

extern "C"
{
	__declspec(dllexport) void* SPIRV_CreateOptimize()
	{
		spvtools::Optimizer* optimizer = new spvtools::Optimizer(SPV_ENV_VULKAN_1_4);
		return optimizer;
	}

	__declspec(dllexport) void SPIRV_DestroyOptimize(void* optimizer)
	{
		delete (spvtools::Optimizer*)optimizer;
	}

	__declspec(dllexport) void SPIRV_OptRegisterPerfPasses(void* optimizer)
	{
		((spvtools::Optimizer*)optimizer)->RegisterPerformancePasses();
	}

	__declspec(dllexport) bool SPIRV_RunOptimize(void* optimizer, SPIRV_OptimizeOut* out)
	{
		spvtools::Optimizer* optimizerObj = (spvtools::Optimizer*)optimizer;
		std::vector<uint32_t> optimizedBinary{};

		if (optimizerObj->Run(out->InBinary, out->InSize, &optimizedBinary))
		{
			out->OutBinary = (uint32_t*)out->Alloc(optimizedBinary.size() * sizeof(uint32_t));
			out->OutSize = optimizedBinary.size() * sizeof(uint32_t);

			memcpy(out->OutBinary, optimizedBinary.data(), optimizedBinary.size() * sizeof(uint32_t));
			return true;
		}
		else
		{
			out->OutBinary = nullptr;
			out->OutSize = 0;

			return false;
		}
	}
}