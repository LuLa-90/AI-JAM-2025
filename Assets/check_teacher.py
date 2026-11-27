
import torch

PATH = "Assets/DatabaseOfRobots/die/Santabumpers-3499966.pt"

print("Loading Teacher checkpoint...")
checkpoint = torch.load(
    PATH,
    map_location="cpu",
    weights_only=False
)

teacher = checkpoint["Policy"]

print("\n=== TEACHER MODEL ===")
print("Global step:", checkpoint["global_step"])
print("Policy version:", teacher["version_number"].item())
print("Continuous actions:", teacher["continuous_act_size_vector"].item())
print("Discrete actions:", teacher["discrete_act_size_vector"].tolist())

print("\n=== NETWORK PARAMETERS ===")
for name, tensor in teacher.items():
    if torch.is_tensor(tensor):
        print(f"{name}: {tuple(tensor.shape)}")

print("\n=== CHECKPOINT COMPONENTS ===")
for name, value in checkpoint.items():
    if name != "Policy":
        print(name, type(value))

print("\nTeacher checkpoint inspection completed.")
