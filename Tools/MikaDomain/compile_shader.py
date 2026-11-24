"""Compile the bundled HLSL Effect with Windows' existing DirectX compiler.
Produces raw fx_2_0 Effect bytecode for FNA, not a Mod build. No downloads.
"""
import ctypes as C
from ctypes import wintypes as W
from pathlib import Path
import hashlib, json, sys

ROOT = Path(__file__).resolve().parents[2]
source = ROOT/'Assets/Skills/MikaDomain'/(sys.argv[1] if len(sys.argv)>1 else 'Domain.fx')
output = source.with_suffix('.fxc')
data = source.read_bytes()
compiler = C.WinDLL('d3dcompiler_47.dll')
compile = compiler.D3DCompile
compile.restype = C.c_long
compile.argtypes = [C.c_void_p,C.c_size_t,C.c_char_p,C.c_void_p,C.c_void_p,
                   C.c_char_p,C.c_char_p,W.UINT,W.UINT,C.POINTER(C.c_void_p),C.POINTER(C.c_void_p)]

def method(p, slot, result):
    table = C.cast(p,C.POINTER(C.POINTER(C.c_void_p))).contents
    return C.WINFUNCTYPE(result,C.c_void_p)(table[slot])

code, errors = C.c_void_p(), C.c_void_p()
try:
    # Optimization level 3. Legacy fx_2_0 container with vs_3_0 + ps_3_0 shaders.
    # NO_PRESHADER keeps uniform math in the GPU program. Legacy effect
    # preshader boolean outputs were not updating correctly on this FNA build.
    hr = compile(data,len(data),source.name.encode(),None,None,None,b'fx_2_0',(1<<15)|(1<<8),0,C.byref(code),C.byref(errors))
    messages = C.string_at(method(errors,3,C.c_void_p)(errors)).decode(errors='replace') if errors else ''
    if messages: print(messages.strip())
    if hr < 0: raise RuntimeError(f'D3DCompile failed: {hr & 0xffffffff:08x}')
    bytecode = C.string_at(method(code,3,C.c_void_p)(code),method(code,4,C.c_size_t)(code))
    assert bytecode[:4] == bytes.fromhex('0109fffe'), 'Expected legacy FNA effect container'
    output.write_bytes(bytecode)
    report = dict(compiler='Windows d3dcompiler_47.dll',profile='fx_2_0; vs_3_0; ps_3_0',
        flags=['optimization_level_3','no_preshader'],
        source_sha256=hashlib.sha256(data).hexdigest(),bytes=len(bytecode),
        bytecode_sha256=hashlib.sha256(bytecode).hexdigest(),messages=messages.strip())
    report_name='shader_validation.json' if source.stem=='PathRibbon' else source.stem+'_shader_validation.json'
    (ROOT/'Tools/MikaDomain'/report_name).write_text(json.dumps(report,indent=2),encoding='utf-8')
    print(f'PASS: {output.name}, {len(bytecode)} bytes; no Mod build performed')
finally:
    if code: method(code,2,W.ULONG)(code)
    if errors: method(errors,2,W.ULONG)(errors)
