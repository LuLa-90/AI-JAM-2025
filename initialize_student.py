import torch
from pathlib import Path

teacher_path = Path("Assets/DatabaseOfRobots/DJeMenTi/Santabumpers-3499966.pt")
output_path = Path("Student_Initialized.pt")

print("Loading Teacher checkpoint...")
checkpoint = torch.load(
    teacher_path,
    map_location="cpu",
    weights_only=False
)

weights = checkpoint["Policy"]

# Uložíme iba naučenú Policy a počítadlo krokov.
# Pôvodný Teacher checkpoint nemeníme.
torch.save(
    {
        "Policy": weights,
        "global_step": checkpoint.get("global_step", 0)
    },
    output_path
)

# Overíme, že nový súbor možno znovu načítať.
verification = torch.load(
    output_path,
    map_location="cpu",
    weights_only=False
)

assert verification["Policy"].keys() == weights.keys()

print("\nSUCCESS!")
print("Student initialization file:", output_path.resolve())
print("Transferred tensors:", len(weights))
print("File size:", round(output_path.stat().st_size / 1024 / 1024, 2), "MB")
