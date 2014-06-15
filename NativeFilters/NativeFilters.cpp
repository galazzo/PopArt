// NativeFilters.cpp
#include "pch.h"
#include <ppltasks.h>
#include <wrl.h>
#include <robuffer.h>
#include <ppltasks.h>
#include <math.h>

#include "NativeFilters.h"

using namespace NativeFilters;
using namespace Platform;
using namespace concurrency;

using namespace Windows::Storage::Streams;
using namespace Microsoft::WRL;

Halftone::Halftone() : m_cell_size(20)
{
}

IAsyncAction^ Halftone::LoadAsync()
{
	return create_async([this]
	{
		unsigned int radius = CellSize;
		unsigned int range = (unsigned int) floor((double)(CellSize / 10));

		for (int i = 0; i < 10; i++)
		{
			tone[i] = new double*[CellSize];
			for (unsigned int j = 0; j < CellSize; j++)
			{
				tone[i][j] = new double[CellSize];
			}

			CreateDot(&tone[i], CellSize, radius);
			radius -= range;
		}
	});
}

void Halftone::CreateDot(double*** dot, unsigned int outersize, unsigned int innersize)
{
	int cx = outersize / 2;
	int cy = outersize / 2;

	double _x = 0;
	double _y = 0;

	double distance = 0;

	innersize /= 2;

	for (int y = 0; y < (int)outersize; y++)
	{
		for (int x = 0; x < (int)outersize; x++)
		{
			_x = x - cx;
			_y = y - cy;

			distance = sqrt(_x * _x + _y * _y);

			if (distance <= innersize)
			{
				(*dot)[x][y] = 0;
			}
			else
			{
				(*dot)[x][y] = 1.0;
			}
		}
	}
}

void Halftone::Process(Windows::Foundation::Rect rect)
{
	unsigned int sourceLength, targetLength;
	byte* sourcePixelRegion = GetPointerToPixelData(sourceBuffer, &sourceLength);
	byte* targetPixelRegion = GetPointerToPixelData(targetBuffer, &targetLength);

	int CellSizeABGR = CellSize * 4;

	unsigned int m_width = (unsigned int) rect.Width*4;
	unsigned int m_height = (unsigned int) rect.Height;
	unsigned int luma = 0;
	unsigned int toneIndex = 0;
	unsigned int xOffset = 0;

	for (unsigned int y = 0; y < m_height - CellSize; y += CellSize)
	{
		for (unsigned int x = 0; x < m_width - CellSizeABGR; x += CellSizeABGR)
		{
			luma = 0;
			for (unsigned int m_y = y; m_y < (y + CellSize); m_y++)
			{
				xOffset = m_y * m_width;
				for (unsigned int m_x = x; m_x < (x + CellSizeABGR); m_x += 4)
				{					
					byte b = (byte)sourcePixelRegion[xOffset + m_x + 0];
					byte g = (byte)sourcePixelRegion[xOffset + m_x + 1];
					byte r = (byte)sourcePixelRegion[xOffset + m_x + 2];
					
					luma += (unsigned int)((double)((int)r + (int)g + (int)b) / 3.0);
				}
			}
			luma /= (CellSize * CellSize);

			toneIndex = (unsigned int) floor((float)luma / 25);
			if (toneIndex >= 10) toneIndex = 9;

			int offset = 8;
			int m_xxx = 0;

			for (unsigned int m_y = y, m_yy = 0; m_y < (y + CellSize); m_y++, m_yy++)
			{
				xOffset = m_y * m_width;
				for (unsigned int m_x = x, m_xx = 0; m_x < (x + CellSizeABGR); m_x+=4, m_xx++)
				{
					m_xxx = (int)((m_xx + ((((y / CellSize) % 2) == 1) ? offset : 0)) % CellSize);

					byte b = (byte)sourcePixelRegion[xOffset + m_x + 0];					
					byte g = (byte)sourcePixelRegion[xOffset + m_x + 1];
					byte r = (byte)sourcePixelRegion[xOffset + m_x + 2];
					
					r = (byte)((double)r * tone[toneIndex][m_yy][m_xxx]);
					g = (byte)((double)g * tone[toneIndex][m_yy][m_xxx]);
					b = (byte)((double)b * tone[toneIndex][m_yy][m_xxx]);

					targetPixelRegion[xOffset + m_x + 0] = b;
					targetPixelRegion[xOffset + m_x + 1] = g;
					targetPixelRegion[xOffset + m_x + 2] = r;
					targetPixelRegion[xOffset + m_x + 3] = 0xFF;
				}
			}
		}
	}
}

IBuffer^ Halftone::ProvideSourceBuffer(Windows::Foundation::Size imageSize)
{
	unsigned int size = (unsigned int)(4 * imageSize.Height * imageSize.Width);
	sourceBuffer = ref new Windows::Storage::Streams::Buffer(size);
	sourceBuffer->Length = size;
	imageWidth = (unsigned int)imageSize.Width;
	return sourceBuffer;
}

IBuffer^ Halftone::ProvideTargetBuffer(Windows::Foundation::Size imageSize)
{
	unsigned int size = (unsigned int)(4 * imageSize.Height * imageSize.Width);
	targetBuffer = ref new Windows::Storage::Streams::Buffer(size);
	targetBuffer->Length = size;
	return targetBuffer;
}

byte* Halftone::GetPointerToPixelData(Windows::Storage::Streams::IBuffer^ pixelBuffer, unsigned int *length)
{
	if (length != nullptr)
	{
		*length = pixelBuffer->Length;
	}
	// Query the IBufferByteAccess interface.
	ComPtr<Windows::Storage::Streams::IBufferByteAccess> bufferByteAccess;
	reinterpret_cast<IInspectable*>(pixelBuffer)->QueryInterface(IID_PPV_ARGS(&bufferByteAccess));

	// Retrieve the buffer data.
	byte* pixels = nullptr;
	bufferByteAccess->Buffer(&pixels);
	return pixels;
}
