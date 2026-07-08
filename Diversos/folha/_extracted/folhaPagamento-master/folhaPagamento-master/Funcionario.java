package folhaPagamento;

public abstract class Funcionario {
	
	String nome;
	String setor;
	String dataAdm;
	
	public Funcionario(String nome, String setor, String dataAdm) {
		this.setNome(nome);
		this.setSetor(setor);
		this.setDataAdm(dataAdm);
	}

	public String getNome() {
		return nome;
	}

	public void setNome(String nome) {
		this.nome = nome;
	}

	public String getSetor() {
		return setor;
	}

	public void setSetor(String setor) {
		this.setor = setor;
	}

	public String getDataAdm() {
		return dataAdm;
	}

	public void setDataAdm(String dataAdm) {
		this.dataAdm = dataAdm;
	}
	
	
}
